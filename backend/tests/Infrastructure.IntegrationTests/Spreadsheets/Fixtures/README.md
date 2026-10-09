# Фикстуры XLSX

`libreoffice-roundtrip.xlsx` — трафарет занятия, сгенерированный `ClosedXmlGradebookSpreadsheet`
с id из `SpreadsheetFixtures`, затем пересохранённый LibreOffice 25.8 (xlsx → ods → xlsx).
LibreOffice переписывает файл целиком и, в частности, понижает `_meta` из `veryHidden` до `hidden`.

Пересоздать: сгенерировать трафарет с теми же константами и выполнить

```bash
soffice --headless --convert-to ods template.xlsx
soffice --headless --convert-to 'xlsx:Calc MS Excel 2007 XML' template.ods
```
