#!/usr/bin/env node
import { readFile, writeFile } from 'fs/promises';
import { fileURLToPath, pathToFileURL } from 'url';
import { dirname, join } from 'path';
import { createRequire } from 'module';

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

const generatorUrl = pathToFileURL(join(generatorDir, 'src', 'app', 'build', 'ksef-pdf-generator.es.js')).href;
const { generateInvoice, generatePDFUPO } = await import(generatorUrl);

const [documentType, inputXmlPath, outputPdfPath, additionalDataJson] = process.argv.slice(2);

if (!documentType || !inputXmlPath || !outputPdfPath) {
    console.error('Użycie: node generate-pdf-wrapper.mjs <invoice|faktura|upo> <inputXml> <outputPdf> [additionalDataJson]');
    process.exit(1);
}

try {
    const xmlBuffer = await readFile(inputXmlPath);
    const xmlFile = new File([xmlBuffer], inputXmlPath.split(/[/\\]/).pop() || 'input.xml', { type: 'application/xml' });

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
    console.log(`PDF wygenerowano: ${outputPdfPath}`);
} catch (error) {
    console.error('Błąd:', error.message);
    process.exit(1);
}
