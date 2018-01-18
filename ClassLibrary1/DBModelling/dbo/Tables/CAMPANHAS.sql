﻿CREATE TABLE [dbo].[CAMPANHAS] (
    [CAMPANHAID]     BIGINT        IDENTITY (1, 1) NOT NULL,
    [CARTEIRAID]     INT           NOT NULL,
    [DATAENVIAR]     SMALLDATETIME NOT NULL,
    [DATA]           DATETIME      NOT NULL,
    [OPERADORAID]    TINYINT           NOT NULL,
    [CLIENTEID]      INT           NOT NULL,
    [USUARIOID]      INT           NULL,
    [FORNECEDORID]   INT           NOT NULL,
    [IDCLIENTE]      VARCHAR (100) SPARSE NULL,
    [STATUSENVIO]    TINYINT       NOT NULL,
    [TIPOCAMPANHAID] INT           NULL,
    [TIPOSMS]        TINYINT       NOT NULL,
    [CELULAR]        NUMERIC (12)  NOT NULL,
    [STATUSREPORT]   TINYINT       NULL,
    [DATAREPORT]     DATETIME      NULL,
    [TEXTO]          VARCHAR (320) NOT NULL,
    [MESSAGEID]      VARCHAR (20)  SPARSE NULL,
    [ARQUIVOID]        INT NULL,
    [DATADIA]        DATE          NOT NULL,
    [DATAENVIOFORNECEDOR] DATETIME NULL, 
	[DDD] AS CAST((SUBSTRING(CAST([CELULAR] AS VARCHAR(11)),(0),(3))) AS TINYINT),
    CONSTRAINT [PK__CAMPANHA__593453D3C31FB19A] PRIMARY KEY CLUSTERED ([CAMPANHAID] ASC, [DATAENVIAR] ASC),
    CONSTRAINT [FK_CAMPANHAS_CARTEIRAS] FOREIGN KEY ([CARTEIRAID]) REFERENCES [dbo].[CARTEIRAS] ([CARTEIRAID]),
	CONSTRAINT [FK_CAMPANHAS_ARQUIVOS] FOREIGN KEY ([ARQUIVOID]) REFERENCES [dbo].[CAMPANHAS_ARQUIVOS] ([ARQUIVOID]) ON DELETE SET NULL ON UPDATE SET NULL,
	CONSTRAINT [FK_CAMPANHAS_CLIENTES] FOREIGN KEY (CLIENTEID) REFERENCES [dbo].CLIENTES (CLIENTEID),
	CONSTRAINT [FK_CAMPANHAS_USUARIOS] FOREIGN KEY (USUARIOID) REFERENCES [dbo].USUARIOS (USUARIOID), --ON DELETE SET NULL ON UPDATE SET NULL,
	CONSTRAINT [FK_CAMPANHAS_FORNECEDOR] FOREIGN KEY (FORNECEDORID) REFERENCES [dbo].FORNECEDOR (FORNECEDORID)
);
GO

CREATE NONCLUSTERED INDEX [ix_CAMPANHAS_CELULAR_includes] ON [dbo].[CAMPANHAS]
(
	[CELULAR] ASC	
)
INCLUDE ([CLIENTEID],[IDCLIENTE]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90)
GO

CREATE NONCLUSTERED INDEX [IX_CAMPANHAS_MONITORIA_filtered]
ON [dbo].[CAMPANHAS] ([CLIENTEID],[DATADIA])
INCLUDE ([CARTEIRAID],[DATAENVIAR],[USUARIOID],[FORNECEDORID],[STATUSENVIO],[ARQUIVOID],[DATAENVIOFORNECEDOR]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

	GO
CREATE NONCLUSTERED INDEX [IX_CAMPANHAS_UPDATES]
ON [dbo].[CAMPANHAS] ([CARTEIRAID],[DATAENVIAR],[CLIENTEID],[ARQUIVOID])
INCLUDE ([CAMPANHAID],[STATUSENVIO])

GO

CREATE NONCLUSTERED INDEX [IX_CAMPANHAS_STATUSCAMPANHAS]
ON [dbo].[CAMPANHAS] ([CLIENTEID],[STATUSENVIO],[DATADIA])
INCLUDE ([CARTEIRAID],[DATAENVIAR],[OPERADORAID],[FORNECEDORID],[IDCLIENTE],[CELULAR],[TEXTO],[ARQUIVOID],[STATUSREPORT],[TIPOCAMPANHAID])

GO

CREATE NONCLUSTERED INDEX [IX_CAMPANHAS_RELATORIO_DETALHADO]
ON [dbo].[CAMPANHAS] ([CLIENTEID],[DATADIA])
INCLUDE ([DATAENVIAR],[OPERADORAID],[IDCLIENTE],[CELULAR],[STATUSREPORT],[DATAREPORT],[TEXTO],STATUSENVIO, CARTEIRAID, ARQUIVOID,TIPOCAMPANHAID)
	
GO

CREATE NONCLUSTERED INDEX [IX_CAMPANHA_PENDENTE]
ON [dbo].[CAMPANHAS] ([STATUSENVIO],[DATADIA])
INCLUDE ([CAMPANHAID],[OPERADORAID],[CLIENTEID],[FORNECEDORID],[CELULAR],[TEXTO])
WHERE ([STATUSENVIO]=1)
GO 