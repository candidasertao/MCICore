﻿CREATE TABLE [dbo].[RETORNO_INTERJEICAO]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [INTERJEICAO] VARCHAR(150) NOT NULL, 
    [CLIENTEID] INT NOT NULL, 
    [CLASSIFICACAO] TINYINT NOT NULL, 
    [USUARIOID] INT NULL, 
)
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_RETORNO_INTERJEICAO ON [dbo].[RETORNO_INTERJEICAO] (INTERJEICAO, CLIENTEID)