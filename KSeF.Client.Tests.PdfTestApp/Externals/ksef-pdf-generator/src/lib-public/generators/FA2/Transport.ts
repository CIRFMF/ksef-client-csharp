import { Content } from 'pdfmake/interfaces';
import { Kraj } from '../../../shared/consts/const';
import {
  createHeader,
  createLabelText,
  createSection,
  createSubHeader,
  formatText,
  generateTwoColumns,
  getTable,
  hasValue,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Adres, Transport } from '../../types/fa2.types';
import {
  getDateTimeWithoutSeconds,
  getOpisTransportuString,
  getRodzajTransportuString,
} from '../../../shared/generators/common/functions';
import { generatePrzewoznik } from './Przewoznik';

export function generateTransport(transport: Transport, index?: number | null): Content {
  const table: Content[] = [];
  const columns = {
    transport: [] as Content[],
    dane: [] as Content[],
    wysylkaZ: [] as Content[],
    wysylkaDo: [] as Content[],
    wysylkaPrzez: [] as Content[],
  };

  table.push(createHeader(index ? `Transport ${index}` : 'Transport'));
  if (transport.RodzajTransportu?._text) {
    columns.transport.push(
      createLabelText('Rodzaj transportu: ', getRodzajTransportuString(transport.RodzajTransportu))
    );
  } else if (transport.TransportInny?._text == '1' && transport.OpisInnegoTransportu?._text) {
    columns.transport.push(createLabelText('Rodzaj transportu: ', 'Transport inny'));
    columns.transport.push(
      createLabelText('Opis innego rodzaju transportu: ', transport.OpisInnegoTransportu)
    );
  }
  columns.dane.push(createLabelText('Numer zlecenia transportu: ', transport.NrZleceniaTransportu));
  if (hasValue(transport.OpisLadunku)) {
    columns.dane.push(createLabelText('Opis ładunku: ', getOpisTransportuString(transport.OpisLadunku)));
    if (transport.LadunekInny?._text === '1' && transport.OpisInnegoLadunku?._text) {
      columns.dane.push(createLabelText('Opis ładunku: ', 'Ładunek inny'));
      columns.dane.push(createLabelText('Opis innego ładunku: ', transport.OpisInnegoLadunku));
    }
  }
  columns.dane.push(createLabelText('Jednostka opakowania: ', transport.JednostkaOpakowania));
  columns.dane.push(
    createLabelText(
      'Data i godzina rozpoczęcia transportu: ',
      getDateTimeWithoutSeconds(transport.DataGodzRozpTransportu)
    )
  );
  columns.dane.push(
    createLabelText(
      'Data i godzina zakończenia transportu: ',
      getDateTimeWithoutSeconds(transport.DataGodzZakTransportu)
    )
  );
  if (columns.dane.length > 0) {
    columns.dane.unshift(createSubHeader('Dane transportu', [0, 0, 0, 0]));
  }
  table.push(generateTwoColumns(columns.transport, columns.dane));

  table.push(generatePrzewoznik(transport.Przewoznik));

  if (transport.WysylkaZ?.AdresL1) {
    columns.wysylkaZ.push(createSubHeader('Adres miejsca wysyłki', [0, 0, 0, 0]));
    columns.wysylkaZ.push(formatText(transport.WysylkaZ?.AdresL1?._text, FormatTyp.Default));
    columns.wysylkaZ.push(formatText(transport.WysylkaZ?.AdresL2?._text, FormatTyp.Default));
    columns.wysylkaZ.push(formatText(Kraj[transport.WysylkaZ?.KodKraju?._text ?? ''], FormatTyp.Default));
    columns.wysylkaZ.push(createLabelText('GLN: ', transport.WysylkaZ?.GLN?._text));
  }

  if (transport.WysylkaDo?.AdresL1) {
    columns.wysylkaDo.push(
      createSubHeader('Adres miejsca docelowego, do którego został zlecony transport', [0, 0, 0, 0])
    );
    columns.wysylkaDo.push(formatText(transport.WysylkaDo?.AdresL1?._text, FormatTyp.Default));
    columns.wysylkaDo.push(formatText(transport.WysylkaDo?.AdresL2?._text, FormatTyp.Default));
    columns.wysylkaDo.push(formatText(Kraj[transport.WysylkaDo?.KodKraju?._text ?? ''], FormatTyp.Default));
    columns.wysylkaDo.push(createLabelText('GLN: ', transport.WysylkaDo?.GLN?._text));
  }

  const wysylkaPrzez: Adres[] = getTable(transport.WysylkaPrzez);

  wysylkaPrzez.forEach((adres: Adres, index: number): void => {
    if (index) {
      columns.wysylkaPrzez.push('\n');
    }
    columns.wysylkaPrzez.push(createSubHeader('Adres pośredni wysyłki', [0, 4, 0, 0]));
    columns.wysylkaPrzez.push(formatText(adres.AdresL1?._text, FormatTyp.Default));
    columns.wysylkaPrzez.push(formatText(adres?.AdresL2?._text, FormatTyp.Default));
    columns.wysylkaPrzez.push(formatText(Kraj[adres?.KodKraju?._text ?? ''], FormatTyp.Default));
    columns.wysylkaPrzez.push(createLabelText('GLN: ', adres?.GLN?._text));
  });

  if (transport.WysylkaZ?.AdresL1 || transport.WysylkaDo?.AdresL1 || transport.WysylkaPrzez?.length) {
    table.push(createHeader('Wysyłka'));
    table.push(generateTwoColumns(columns.wysylkaZ, columns.wysylkaDo));
    table.push(generateTwoColumns(columns.wysylkaPrzez, []));
  }
  return createSection(table, true);
}
