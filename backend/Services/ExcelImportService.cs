using System.Data;
using ExcelDataReader;
using backend.Data;
using shared.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ExcelImportService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ExcelImportService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<(int inserted, int updated, List<string> errors)> ImportJigSpecsAsync(Stream fileStream, string fileName)
        {
            var errors = new List<string>();
            int inserted = 0;
            int updated = 0;

            try
            {
                using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                    ? ExcelReaderFactory.CreateCsvReader(fileStream) 
                    : ExcelReaderFactory.CreateReader(fileStream);
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count == 0)
                {
                    errors.Add("No worksheets found in the Excel file.");
                    return (inserted, updated, errors);
                }

                var table = result.Tables[0];
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    columnNames[table.Columns[i].ColumnName.Trim()] = i;
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                var existingSpecs = await context.JigSpecs.ToDictionaryAsync(s => s.Id);

                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        string id = GetString(row, columnNames, "ID", "Spec ID", "SpecId");
                        if (string.IsNullOrWhiteSpace(id)) continue; // Skip empty rows

                        string name = GetString(row, columnNames, "Name", "Description");
                        if (string.IsNullOrWhiteSpace(name)) name = id; // Fallback

                        string week = GetString(row, columnNames, "Week");
                        string item = GetString(row, columnNames, "Item");
                        string rev = GetString(row, columnNames, "Rev", "Revision");
                        string toyNumber = GetString(row, columnNames, "ToyNumber", "Toy Number");
                        string partNumber = GetString(row, columnNames, "PartNumber", "Part Number");
                        string partType = GetString(row, columnNames, "PartType", "Part Type");
                        string jigType = GetString(row, columnNames, "JigType", "JIG Type", "Jig Type");
                        string toolNo = GetString(row, columnNames, "ToolNo", "NO.Tool", "Tool No");
                        string toolType = GetString(row, columnNames, "ToolType", "Tool Type");
                        string totalStepPrint = GetString(row, columnNames, "TotalStepPrint", "Total Step Print", "Step Print");
                        string unitAmount = GetString(row, columnNames, "UnitAmount", "Unit Amount", "จำนวน/บอร์ด", "จำนวน");
                        string feed = GetString(row, columnNames, "Feed");
                        string scan = GetString(row, columnNames, "Scan");
                        string pictureUrl = GetString(row, columnNames, "PictureUrl", "Picture URL", "Picture");
                        
                        int jigRequired = 1;
                        string reqStr = GetString(row, columnNames, "JigRequired", "Required Quantity", "Required");
                        if (int.TryParse(reqStr, out int req)) jigRequired = req;

                        if (existingSpecs.TryGetValue(id, out var spec))
                        {
                            spec.Name = name;
                            spec.Week = week;
                            spec.Item = item;
                            spec.Rev = rev;
                            spec.ToyNumber = toyNumber;
                            spec.PartNumber = partNumber;
                            spec.PartType = partType;
                            spec.JigType = jigType;
                            spec.ToolNo = toolNo;
                            spec.ToolType = toolType;
                            spec.TotalStepPrint = totalStepPrint;
                            spec.UnitAmount = unitAmount;
                            spec.Feed = feed;
                            spec.Scan = scan;
                            spec.PictureUrl = pictureUrl;
                            spec.JigRequired = jigRequired;
                            updated++;
                        }
                        else
                        {
                            spec = new JigSpec
                            {
                                Id = id,
                                Name = name,
                                Week = week,
                                Item = item,
                                Rev = rev,
                                ToyNumber = toyNumber,
                                PartNumber = partNumber,
                                PartType = partType,
                                JigType = jigType,
                                ToolNo = toolNo,
                                ToolType = toolType,
                                TotalStepPrint = totalStepPrint,
                                UnitAmount = unitAmount,
                                Feed = feed,
                                Scan = scan,
                                PictureUrl = pictureUrl,
                                JigRequired = jigRequired
                            };
                            context.JigSpecs.Add(spec);
                            existingSpecs[id] = spec;
                            inserted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error processing row: {ex.Message}");
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to process Excel file: {ex.Message}");
            }

            return (inserted, updated, errors);
        }

        public async Task<(int inserted, int updated, List<string> errors)> ImportPhysicalJigsAsync(Stream fileStream, string fileName)
        {
            var errors = new List<string>();
            int inserted = 0;
            int updated = 0;

            try
            {
                using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                    ? ExcelReaderFactory.CreateCsvReader(fileStream) 
                    : ExcelReaderFactory.CreateReader(fileStream);
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count == 0)
                {
                    errors.Add("No worksheets found in the Excel file.");
                    return (inserted, updated, errors);
                }

                var table = result.Tables[0];
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    columnNames[table.Columns[i].ColumnName.Trim()] = i;
                }

                using var context = await _dbFactory.CreateDbContextAsync();
                var existingJigs = await context.PhysicalJigs.ToDictionaryAsync(j => j.Id);
                var existingSpecs = await context.JigSpecs.Select(s => s.Id).ToHashSetAsync();
                var existingLocators = await context.Locators.ToDictionaryAsync(l => l.Id);

                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        string id = GetString(row, columnNames, "ID", "Jig ID", "JigId");
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        string specId = GetString(row, columnNames, "SpecId", "Spec ID", "Spec");
                        string status = GetString(row, columnNames, "Status", "สถานะ");
                        if (string.IsNullOrWhiteSpace(status)) status = "Available";

                        string locatorId = GetString(row, columnNames, "LocatorId", "Locator ID", "Location", "Locator");
                        string condition = GetString(row, columnNames, "Condition", "สภาพ");
                        if (string.IsNullOrWhiteSpace(condition)) condition = "Good";

                        string tool = GetString(row, columnNames, "Tool");
                        string namePlateBlack = GetString(row, columnNames, "NamePlateBlack", "Name Plate ดำ", "ป้ายดำ");
                        string namePlateWhite = GetString(row, columnNames, "NamePlateWhite", "Name Plate ขาว", "ป้ายขาว");
                        string part = GetString(row, columnNames, "Part");
                        string jigType = GetString(row, columnNames, "JigType", "ชนิด JIG");
                        
                        string stepPrint = GetString(row, columnNames, "StepPrint", "Step print");
                        string hg = GetString(row, columnNames, "HG", "H/G");
                        string fs = GetString(row, columnNames, "FS", "F/S");
                        string issueDate = GetString(row, columnNames, "IssueDate", "วันที่เบิก", "Date");
                        string jigCapacity = GetString(row, columnNames, "JigCapacity", "จำนวนวัตถุต่อ JIG", "Capacity");

                        // Auto-create locator if not exists
                        if (!string.IsNullOrWhiteSpace(locatorId) && !existingLocators.ContainsKey(locatorId))
                        {
                            var loc = new Locator { Id = locatorId, Site = "MBK", Cabinet = "1", Shelf = "1", Position = "1" };
                            context.Locators.Add(loc);
                            existingLocators[locatorId] = loc;
                        }

                        if (existingJigs.TryGetValue(id, out var jig))
                        {
                            if (!string.IsNullOrWhiteSpace(specId)) jig.SpecId = specId;
                            jig.Status = status;
                            jig.LocatorId = locatorId;
                            jig.Condition = condition;
                            jig.Tool = tool;
                            jig.NamePlateBlack = namePlateBlack;
                            jig.NamePlateWhite = namePlateWhite;
                            jig.Part = part;
                            jig.JigType = jigType;
                            jig.StepPrint = stepPrint;
                            jig.HG = hg;
                            jig.FS = fs;
                            jig.IssueDate = issueDate;
                            jig.JigCapacity = jigCapacity;
                            updated++;
                        }
                        else
                        {
                            jig = new PhysicalJig
                            {
                                Id = id,
                                SpecId = specId ?? "",
                                Status = status,
                                LocatorId = locatorId ?? "",
                                Condition = condition,
                                Tool = tool,
                                NamePlateBlack = namePlateBlack,
                                NamePlateWhite = namePlateWhite,
                                Part = part,
                                JigType = jigType,
                                StepPrint = stepPrint,
                                HG = hg,
                                FS = fs,
                                IssueDate = issueDate,
                                JigCapacity = jigCapacity
                            };
                            context.PhysicalJigs.Add(jig);
                            existingJigs[id] = jig;
                            inserted++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error processing row: {ex.Message}");
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to process Excel file: {ex.Message}");
            }

            return (inserted, updated, errors);
        }

        public async Task<(int inserted, int updated, List<string> errors)> ImportLocatorsAsync(Stream fileStream, string fileName)
        {
            var errors = new List<string>();
            int inserted = 0;
            int updated = 0;

            try
            {
                using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                    ? ExcelReaderFactory.CreateCsvReader(fileStream) 
                    : ExcelReaderFactory.CreateReader(fileStream);
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                if (result.Tables.Count == 0) return (inserted, updated, new List<string> { "No worksheets found." });

                var table = result.Tables[0];
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < table.Columns.Count; i++) columnNames[table.Columns[i].ColumnName.Trim()] = i;

                using var context = await _dbFactory.CreateDbContextAsync();
                var existingLocators = await context.Locators.ToDictionaryAsync(l => l.Id);

                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        string id = GetString(row, columnNames, "ID", "Locator ID", "Location ID", "Locator");
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        string site = GetString(row, columnNames, "Site", "ไซต์", "Location");
                        string cabinet = GetString(row, columnNames, "Cabinet", "ตู้");
                        string shelf = GetString(row, columnNames, "Shelf", "ชั้น");
                        string position = GetString(row, columnNames, "Position", "ตำแหน่ง", "ตัว");

                        if (existingLocators.TryGetValue(id, out var loc))
                        {
                            loc.Site = site;
                            loc.Cabinet = cabinet;
                            loc.Shelf = shelf;
                            loc.Position = position;
                            updated++;
                        }
                        else
                        {
                            loc = new Locator { Id = id, Site = site, Cabinet = cabinet, Shelf = shelf, Position = position };
                            context.Locators.Add(loc);
                            existingLocators[id] = loc;
                            inserted++;
                        }
                    }
                    catch (Exception ex) { errors.Add($"Row error: {ex.Message}"); }
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex) { errors.Add($"File error: {ex.Message}"); }
            return (inserted, updated, errors);
        }

        public async Task<(int inserted, int updated, List<string> errors)> ImportPartMappingsAsync(Stream fileStream, string fileName)
        {
            var errors = new List<string>();
            int inserted = 0;

            try
            {
                using var reader = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) 
                    ? ExcelReaderFactory.CreateCsvReader(fileStream) 
                    : ExcelReaderFactory.CreateReader(fileStream);
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                if (result.Tables.Count == 0) return (inserted, 0, new List<string> { "No worksheets found." });

                var table = result.Tables[0];
                var columnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < table.Columns.Count; i++) columnNames[table.Columns[i].ColumnName.Trim()] = i;

                using var context = await _dbFactory.CreateDbContextAsync();
                
                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        string partNumber = GetString(row, columnNames, "PartNumber", "Part Number", "Part");
                        string specId = GetString(row, columnNames, "SpecId", "Spec ID", "Spec");
                        
                        if (string.IsNullOrWhiteSpace(partNumber) || string.IsNullOrWhiteSpace(specId)) continue;
                        
                        // Check if exists
                        if (!await context.PartJigMappings.AnyAsync(m => m.PartNumber == partNumber && m.SpecId == specId))
                        {
                            context.PartJigMappings.Add(new PartJigMapping { PartNumber = partNumber, SpecId = specId });
                            inserted++;
                        }
                    }
                    catch (Exception ex) { errors.Add($"Row error: {ex.Message}"); }
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex) { errors.Add($"File error: {ex.Message}"); }
            return (inserted, 0, errors);
        }

        private string GetString(DataRow row, Dictionary<string, int> columnNames, params string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                if (columnNames.TryGetValue(name, out int index))
                {
                    var val = row[index];
                    return val == DBNull.Value ? "" : val.ToString()?.Trim() ?? "";
                }
            }
            return "";
        }
    }
}
