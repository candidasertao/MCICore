﻿CREATE TABLE [dbo].[FORNECEDOR_CAMPANHAS]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [DATAENVIAR] SMALLDATETIME NOT NULL, 
    [FORNECEDORID] INT NOT NULL, 
    [CLIENTEID] INT NOT NULL, 
    [USUARIOID] INT NULL, 
    [CARTEIRAID] INT NOT NULL, 
    [ARQUIVOID] INT NULL, 
    [QUANTIDADE] INT NOT NULL
)
