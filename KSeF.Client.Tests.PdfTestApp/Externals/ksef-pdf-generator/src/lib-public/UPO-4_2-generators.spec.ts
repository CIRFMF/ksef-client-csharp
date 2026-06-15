import pdfMake from 'pdfmake/build/pdfmake';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePDFUPO } from './UPO-4_2-generators';
import * as XMLParser from '../shared/XML-parser';

describe('generatePDFUPO', () => {
  const dummyFile = new File(['dummy'], 'dummy.xml', { type: 'text/xml' });
  const dummyUpo = {
    Potwierdzenie: {
      field1: 'value1',
      field2: 'value2',
    },
  };

  beforeEach(() => {
    vi.spyOn(XMLParser, 'parseXML').mockResolvedValue(dummyUpo);

    vi.spyOn(pdfMake, 'createPdf').mockImplementation(
      () =>
        ({
          getBlob: (callback: (blob: Blob | null) => void) => {
            const blob = new Blob(['PDF content'], { type: 'application/pdf' });

            callback(blob);
          },
        }) as any
    );
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('successfully generates a PDF blob', async () => {
    const blob = await generatePDFUPO(dummyFile);

    expect(blob).toBeInstanceOf(Blob);
    const text = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();

      reader.onload = (): void => resolve(reader.result as string);
      reader.onerror = (): void => reject(reader.error);
      reader.readAsText(blob);
    });

    expect(text).toContain('PDF content');
  });

  it('rejects promise if pdfMake returns null blob', async () => {
    vi.spyOn(pdfMake, 'createPdf').mockReturnValue({
      getBlob: (callback: (blob: Blob | null) => void) => {
        callback(null);
      },
    } as any);

    await expect(generatePDFUPO(dummyFile)).rejects.toEqual('Error');
  });

  it('calls parseXML with the input file', async () => {
    const parseXMLSpy = vi.spyOn(XMLParser, 'parseXML');

    await generatePDFUPO(dummyFile);
    expect(parseXMLSpy).toHaveBeenCalledWith(dummyFile);
  });
});
