﻿CREATE TABLE [dbo].[FORNECEDOR_CLIENTE_OCORRENCIAS]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [FORNECEDORID] INT NOT NULL CONSTRAINT FK_FORNECEDOR_CLIENTE_OCORRENCIAS_FORNECEDORID FOREIGN KEY ([FORNECEDORID]) REFERENCES FORNECEDOR([FORNECEDORID]), 
    [CLIENTEID] INT NOT NULL CONSTRAINT FK_FORNECEDOR_CLIENTE_OCORRENCIAS_CLIENTE FOREIGN KEY ([FORNECEDORID]) REFERENCES CLIENTES (CLIENTEID), 
    [DATA] DATETIME NOT NULL, 
    [TIPO] TINYINT NOT NULL, 
    [DESCRICAO] VARCHAR(300) NOT NULL, 
    [PONTUACAO] DECIMAL(9, 2) NOT NULL
)