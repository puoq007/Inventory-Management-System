ALTER TABLE JigSpecs ADD [Rev] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [PictureUrl] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [ToyNumber] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [PartNumber] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [PartType] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [JigType] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [ToolNo] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [ToolType] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [TotalStepPrint] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [UnitAmount] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [Feed] nvarchar(max) DEFAULT '';
ALTER TABLE JigSpecs ADD [Scan] nvarchar(max) DEFAULT '';

UPDATE JigSpecs SET [Rev] = '', [PictureUrl] = '', [ToyNumber] = '', [PartNumber] = '', [PartType] = '', [JigType] = '', [ToolNo] = '', [ToolType] = '', [TotalStepPrint] = '', [UnitAmount] = '', [Feed] = '', [Scan] = '';
