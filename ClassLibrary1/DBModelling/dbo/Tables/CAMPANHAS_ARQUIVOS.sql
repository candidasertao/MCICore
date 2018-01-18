﻿CREATE TABLE [dbo].[CAMPANHAS_ARQUIVOS]
(
	[ARQUIVOID] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ARQUIVO] VARCHAR(255) NOT NULL, 
    [CLIENTEID] INT NOT NULL, 
    [USUARIOID] INT NULL, 
    [DATA] DATETIME NOT NULL, 
)
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_CAMPANHAS_ARQUIVOS_ARQUIVO_CLIENTEID] ON [dbo].[CAMPANHAS_ARQUIVOS] (ARQUIVO,CLIENTEID)