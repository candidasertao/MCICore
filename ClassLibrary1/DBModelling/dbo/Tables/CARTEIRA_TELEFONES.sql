﻿CREATE TABLE [dbo].[CARTEIRA_TELEFONES]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [CARTEIRAID] INT NOT NULL, 
    [NUMERO] NUMERIC(11) NOT NULL, 
    [DESCRICAO] VARCHAR(20) NULL, 
    CONSTRAINT [FK_CARTEIRA_TELEFONES_CARTEIRA] FOREIGN KEY (CARTEIRAID) REFERENCES CARTEIRAS(CARTEIRAID) ON DELETE CASCADE ON UPDATE CASCADE
)
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_CARTEIRA_TELEFONE]
    ON [dbo].CARTEIRA_TELEFONES(CARTEIRAID ASC, NUMERO ASC);