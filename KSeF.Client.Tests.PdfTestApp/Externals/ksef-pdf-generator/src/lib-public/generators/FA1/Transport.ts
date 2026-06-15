import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  createSection,
  createSubHeader,
  generateTwoColumns,
  getTable,
  hasValue,
} from '../../../shared/PDF-functions';
import { Transport } from '../../types/fa1.types';
import {
  getDateTimeWithoutSeconds,
  getOpisTransportuString,
  getRodzajTransportuString,
} from '../../../shared/generators/common/functions';
import { generateAdres } from './Adres';
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

  if (transport.WysylkaZ) {
    columns.wysylkaZ.push(createSubHeader('Adres miejsca wysyłki', [0, 0, 0, 0]));
    columns.wysylkaZ.push(generateAdres(transport.WysylkaZ));
  }

  if (transport.WysylkaDo) {
    columns.wysylkaDo.push(
      createSubHeader('Adres miejsca docelowego, do którego został zlecony transport', [0, 0, 0, 0])
    );
    columns.wysylkaDo.push(generateAdres(transport.WysylkaDo));
  }

  const wysylkaPrzez = getTable(transport.WysylkaPrzez);

  wysylkaPrzez.forEach((adres, index) => {
    if (index) {
      columns.wysylkaPrzez.push('\n');
    }
    columns.wysylkaPrzez.push(createSubHeader('Adres pośredni wysyłki', [0, 4, 0, 0]));
    columns.wysylkaPrzez.push(generateAdres(adres));
  });

  if (transport.WysylkaZ || transport.WysylkaDo || transport.WysylkaPrzez?.length) {
    table.push(createHeader('Wysyłka'));
    table.push(generateTwoColumns(columns.wysylkaZ, columns.wysylkaDo));
    table.push(generateTwoColumns(columns.wysylkaPrzez, []));
  }
  return createSection(table, true);
}
