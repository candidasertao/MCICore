﻿CREATE TABLE [dbo].[REQUISICAO_RELATORIO_CARTEIRAS]
(
	[CODIGO] INT NOT NULL PRIMARY KEY IDENTITY, 
    [REQUISICAOID] INT NOT NULL, 
    [CARTEIRAID] INT NOT NULL, 
    CONSTRAINT [FK_REQUISICAO_RELATORIO_CARTEIRAS_REQUSICAO_RELATORIO] FOREIGN KEY (REQUISICAOID) REFERENCES REQUISICAO_RELATORIO(REQUISICAOID) ON DELETE CASCADE, 
    CONSTRAINT [FK_REQUISICAO_RELATORIO_CARTEIRAS_CARTEIRAS] FOREIGN KEY (CARTEIRAID) REFERENCES CARTEIRAS(CARTEIRAID) ON DELETE CASCADE

)
