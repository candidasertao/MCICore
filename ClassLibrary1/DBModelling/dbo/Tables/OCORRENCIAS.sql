﻿CREATE TABLE [dbo].[OCORRENCIAS]
(
	[CODIGO] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[TIPOID] INT NOT NULL,
	[DATA] DATETIME NOT NULL,
	[FORNECEDORID] INT NULL,
	[CLIENTEID] INT NULL
)