﻿/*
Deployment script for moneosi

This code was generated by a tool.
Changes to this file may cause incorrect behavior and will be lost if
the code is regenerated.
*/

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO
:setvar DatabaseName "moneosi"
:setvar DefaultFilePrefix "moneosi"
:setvar DefaultDataPath "D:\RDSDBDATA\DATA\"
:setvar DefaultLogPath "D:\RDSDBDATA\DATA\"

GO
:on error exit
GO
/*
Detect SQLCMD mode and disable script execution if SQLCMD mode is not supported.
To re-enable the script after enabling SQLCMD mode, execute the following:
SET NOEXEC OFF; 
*/
:setvar __IsSqlCmdEnabled "True"
GO
IF N'$(__IsSqlCmdEnabled)' NOT LIKE N'True'
    BEGIN
        PRINT N'SQLCMD mode must be enabled to successfully execute this script.';
        SET NOEXEC ON;
    END


GO
USE [$(DatabaseName)];


GO
PRINT N'Rename refactoring operation with key 66c1940a-19ca-45b8-a80e-afd2de9f23d8 is skipped, element [dbo].[CONTATOS_FORNECEDOR].[Id] (SqlSimpleColumn) will not be renamed to CODIGO';


GO
PRINT N'Altering [dbo].[CAMPANHAS]...';


GO
ALTER TABLE [dbo].[CAMPANHAS] DROP COLUMN [DDD];


GO
ALTER TABLE [dbo].[CAMPANHAS]
    ADD [DDD] AS CAST ((SUBSTRING(CAST ([CELULAR] AS VARCHAR (11)), (0), (3))) AS TINYINT);


GO
PRINT N'Altering [dbo].[FORNECEDOR]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

UPDATE [dbo].[FORNECEDOR]
SET    [NOME] = ''
WHERE  [NOME] IS NULL;

ALTER TABLE [dbo].[FORNECEDOR] ALTER COLUMN [NOME] VARCHAR (150) NOT NULL;

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'Creating [dbo].[CONTATOS_FORNECEDOR]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
CREATE TABLE [dbo].[CONTATOS_FORNECEDOR] (
    [CODIGO]       INT           IDENTITY (1, 1) NOT NULL,
    [EMAIL]        VARCHAR (150) NULL,
    [TELEFONE]     DECIMAL (12)  NULL,
    [DESCRICAO]    VARCHAR (150) NULL,
    [FORNECEDORID] INT           NOT NULL,
    PRIMARY KEY CLUSTERED ([CODIGO] ASC)
);


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Creating [dbo].[FK_CONTATOS_FORNECEDOR_FORNECEDOR]...';


GO
ALTER TABLE [dbo].[CONTATOS_FORNECEDOR] WITH NOCHECK
    ADD CONSTRAINT [FK_CONTATOS_FORNECEDOR_FORNECEDOR] FOREIGN KEY ([FORNECEDORID]) REFERENCES [dbo].[FORNECEDOR] ([FORNECEDORID]);


GO
PRINT N'Refreshing [dbo].[FORNECEDORES_CLIENTE]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
EXECUTE sp_refreshsqlmodule N'[dbo].[FORNECEDORES_CLIENTE]';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Refreshing [dbo].[HIGIENIZA]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
EXECUTE sp_refreshsqlmodule N'[dbo].[HIGIENIZA]';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Refreshing [dbo].[CAMPANHA_RETORNO_PAGINACAO]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
EXECUTE sp_refreshsqlmodule N'[dbo].[CAMPANHA_RETORNO_PAGINACAO]';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Refreshing [dbo].[CONSOLIDADO_CAMPANHAS_AGENDADA]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
EXECUTE sp_refreshsqlmodule N'[dbo].[CONSOLIDADO_CAMPANHAS_AGENDADA]';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Refreshing [dbo].[CONSOLIDADO_STATUS_PAGINACAO]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
EXECUTE sp_refreshsqlmodule N'[dbo].[CONSOLIDADO_STATUS_PAGINACAO]';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
-- Refactoring step to update target server with deployed transaction logs
IF NOT EXISTS (SELECT OperationKey FROM [dbo].[__RefactorLog] WHERE OperationKey = '66c1940a-19ca-45b8-a80e-afd2de9f23d8')
INSERT INTO [dbo].[__RefactorLog] (OperationKey) values ('66c1940a-19ca-45b8-a80e-afd2de9f23d8')

GO

GO
PRINT N'Checking existing data against newly created constraints';


GO
USE [$(DatabaseName)];


GO
ALTER TABLE [dbo].[CONTATOS_FORNECEDOR] WITH CHECK CHECK CONSTRAINT [FK_CONTATOS_FORNECEDOR_FORNECEDOR];


GO
PRINT N'Update complete.';


GO
