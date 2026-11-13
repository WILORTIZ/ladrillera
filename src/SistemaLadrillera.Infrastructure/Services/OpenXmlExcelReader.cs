using System.IO.Compression;
using System.Xml.Linq;

namespace SistemaLadrillera.Infrastructure.Services;

public static class OpenXmlExcelReader
{
    public static Dictionary<string, List<Dictionary<string, string>>> ReadWorkbook(string filePath)
    {
        var result = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(filePath)) return result;

        using var archive = ZipFile.OpenRead(filePath);
        var sharedStrings = new List<string>();

        var ssEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (ssEntry != null)
        {
            using var s = ssEntry.Open();
            var doc = XDocument.Load(s);
            foreach (var si in doc.Descendants().Where(e => e.Name.LocalName == "si"))
            {
                var texts = si.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value);
                sharedStrings.Add(string.Concat(texts));
            }
        }

        var wbEntry = archive.GetEntry("xl/workbook.xml");
        var relEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (wbEntry == null || relEntry == null) return result;

        var relsDoc = XDocument.Load(relEntry.Open());
        var relMap = relsDoc.Descendants()
            .Where(e => e.Name.LocalName == "Relationship")
            .ToDictionary(
                e => e.Attribute("Id")?.Value ?? "",
                e => e.Attribute("Target")?.Value ?? "");

        var wbDoc = XDocument.Load(wbEntry.Open());
        foreach (var sheet in wbDoc.Descendants().Where(e => e.Name.LocalName == "sheet"))
        {
            string sheetName = sheet.Attribute("name")?.Value ?? "";
            string rId = sheet.Attributes().FirstOrDefault(a => a.Name.LocalName == "id")?.Value ?? "";
            if (!relMap.TryGetValue(rId, out string? target) || string.IsNullOrEmpty(target))
                continue;

            string entryPath = "xl/" + target.TrimStart('/');
            var shEntry = archive.GetEntry(entryPath);
            if (shEntry == null) continue;

            var shDoc = XDocument.Load(shEntry.Open());
            var rowsList = new List<Dictionary<string, string>>();
            foreach (var row in shDoc.Descendants().Where(e => e.Name.LocalName == "row"))
            {
                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in row.Elements().Where(e => e.Name.LocalName == "c"))
                {
                    string cellRef = c.Attribute("r")?.Value ?? "";
                    string colLetters = new string(cellRef.TakeWhile(char.IsLetter).ToArray());
                    string cellType = c.Attribute("t")?.Value ?? "";
                    string val = "";

                    var vElem = c.Elements().FirstOrDefault(e => e.Name.LocalName == "v");
                    var isElem = c.Elements().FirstOrDefault(e => e.Name.LocalName == "is");
                    if (vElem != null)
                    {
                        val = vElem.Value;
                        if (cellType == "s" && int.TryParse(val, out int idx) && idx >= 0 && idx < sharedStrings.Count)
                        {
                            val = sharedStrings[idx];
                        }
                    }
                    else if (isElem != null)
                    {
                        val = string.Concat(isElem.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value));
                    }

                    if (!string.IsNullOrEmpty(colLetters))
                    {
                        rowDict[colLetters] = val?.Trim() ?? "";
                    }
                }
                if (rowDict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    rowsList.Add(rowDict);
                }
            }
            result[sheetName] = rowsList;
        }

        return result;
    }
}

