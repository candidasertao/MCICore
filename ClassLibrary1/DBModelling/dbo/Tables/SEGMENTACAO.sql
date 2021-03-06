﻿CREATE TABLE [dbo].[SEGMENTACAO] (
    [CODIGO]    INT           IDENTITY (1, 1) NOT NULL,
    [NOME]      VARCHAR (150) NOT NULL,
    [CLIENTEID] INT           NOT NULL,
    PRIMARY KEY CLUSTERED ([CODIGO] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_SEGMENTACAO]
    ON [dbo].[SEGMENTACAO]([NOME] ASC, [CLIENTEID] ASC);

