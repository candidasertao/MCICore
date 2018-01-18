﻿CREATE TABLE [dbo].[IDENTITYSERVER_CLIENTS]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [SECRET] VARCHAR(50) NOT NULL, 
    [CLIENTEID] VARCHAR(50) NOT NULL, 
    [TOKENTYPE] TINYINT NOT NULL, 
    [GRANTYPES] VARCHAR(50) NOT NULL, 
    [TOKENLIFE] INT NOT NULL
)

GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_IDENTITYSERVER_CLIENTS_CLIENTEID] ON [dbo].[IDENTITYSERVER_CLIENTS] (CLIENTEID)