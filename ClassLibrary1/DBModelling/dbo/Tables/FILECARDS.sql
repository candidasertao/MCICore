﻿CREATE TABLE [dbo].[FILECARDS]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [GUID] VARCHAR(40) NOT NULL, 
    [DATA] DATETIME NOT NULL, 
    [ARQUIVO] VARCHAR(250) NOT NULL 
)

GO