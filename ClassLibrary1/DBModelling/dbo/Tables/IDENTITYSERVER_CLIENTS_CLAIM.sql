﻿CREATE TABLE [dbo].[IDENTITYSERVER_CLIENTS_CLAIM]
(
	[ID] INT NOT NULL PRIMARY KEY IDENTITY, 
    [CODIGO] INT NOT NULL, 
    [VALUE] VARCHAR(50) NOT NULL, 
    [TYPE] VARCHAR(250) NOT NULL,
	CONSTRAINT [FK_IDENTITYSERVER_CLIENTS_CLAIM_IDENTITYSERVER_CLIENTS] FOREIGN KEY (CODIGO) REFERENCES IDENTITYSERVER_CLIENTS(CODIGO)
)
