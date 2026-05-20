#!/usr/bin/env node
import { readFile, writeFile, stat, readdir } from 'fs/promises';
import { fileURLToPath, pathToFileURL } from 'url';
import { dirname, join } from 'path';
import { createRequire } from 'module';
import * as readline from 'readline';

const __dirname = dirname(fileURLToPath(import.meta.url));
const generatorDir = join(__dirname, 'Externals', 'ksef-pdf-generator');
const require = createRequire(join(generatorDir, 'package.json'));

const { JSDOM } = require('jsdom');
const dom = new JSDOM('<!DOCTYPE html><html><body></body></html>', {
    url: 'http://localhost',
    pretendToBeVisual: true,
    resources: 'usable'
});

global.window = dom.window;
global.document = dom.window.document;
global.File = dom.window.File;
global.Blob = dom.window.Blob;
global.FileReader = dom.window.FileReader;

try {
    Object.defineProperty(global, 'navigator', {
        value: dom.window.navigator,
        writable: true,
        configurable: true
    });
} catch { }

const pdfMake = require(join(generatorDir, 'node_modules', 'pdfmake', 'build', 'pdfmake.js'));
const vfs = require(join(generatorDir, 'node_modules', 'pdfmake', 'build', 'vfs_fonts.js'));
pdfMake.vfs = vfs;
global.pdfMake = pdfMake;

const generatorUrl = pathToFileURL(join(generatorDir, 'dist', 'ksef-fe-invoice-converter.js')).href;
const { generateInvoice, generatePDFUPO } = await import(generatorUrl);

const [documentType, inputXmlPath, outputPdfPath, additionalDataJson] = process.argv.slice(2);

if (documentType && inputXmlPath && !outputPdfPath) {
    const fileInfo = await stat(inputXmlPath).catch(() => null);

    if (fileInfo?.isDirectory()) {
        const files = await readdir(inputXmlPath);
        const xmlFiles = files.filter(f => f.toLowerCase().endsWith('.xml'));

        if (xmlFiles.length === 0) {
            console.error(`Brak plików XML w folderze: ${inputXmlPath}`);
            process.exit(1);
        }

        let hasErrors = false;
        for (const file of xmlFiles) {
            const fullInput = join(inputXmlPath, file);
            const fullOutput = join(inputXmlPath, file.replace(/\.xml$/i, '.pdf'));
            const ok = await processXml({
                documentType,
                inputXmlPath: fullInput,
                outputPdfPath: fullOutput,
                additionalDataJson
            });
            if (!ok) hasErrors = true;
        }

        if (hasErrors) process.exit(1);
        process.exit(0);
    }
}

if (documentType && inputXmlPath && outputPdfPath) {
    await processXml({ documentType, inputXmlPath, outputPdfPath, additionalDataJson });
    process.exit(0);
}

const rl = readline.createInterface({ input: process.stdin });
let hasErrors = false;

for await (const line of rl) {
    const trimmed = line.trim();
    if (!trimmed) continue;

    let task;
    try {
        task = JSON.parse(trimmed);
    } catch {
        console.error(`Pominięto nieprawidłowy JSON: ${trimmed}`);
        hasErrors = true;
        continue;
    }

    const ok = await processXml(task);
    if (!ok) hasErrors = true;
}

async function processXml({ documentType, inputXmlPath, outputPdfPath, additionalDataJson }) {
    if (!documentType || !inputXmlPath || !outputPdfPath) {
        console.error(`Błąd: brak wymaganych pól (documentType, inputXmlPath, outputPdfPath)`);
        return false;
    }
    try {
        const xmlBuffer = await readFile(inputXmlPath);
        const xmlFile = new File(
            [xmlBuffer],
            inputXmlPath.split(/[/\\]/).pop() || 'input.xml',
            { type: 'application/xml' }
        );

        const docType = documentType.toLowerCase();
        const isInvoice = docType === 'invoice' || docType === 'faktura';

        const pdfBlob = isInvoice
            ? await generateInvoice(xmlFile, additionalDataJson ? JSON.parse(additionalDataJson) : {}, 'blob')
            : await generatePDFUPO(xmlFile);

        const buffer = await new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(Buffer.from(reader.result));
            reader.onerror = reject;
            reader.readAsArrayBuffer(pdfBlob);
        });

        await writeFile(outputPdfPath, buffer);
        console.log(`OK: ${outputPdfPath}`);
        return true;
    } catch (error) {
        console.error(`\nNie udało się wygenerować PDF:`);
        console.error(`  Plik:  ${inputXmlPath}`);
        console.error(`  Błąd:  ${error.message}\n`);
        return false;
    }
}