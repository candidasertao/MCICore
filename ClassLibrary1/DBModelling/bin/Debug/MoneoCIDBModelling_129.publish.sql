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
/*
The column [dbo].[LOG_ATIVIDADE].[MODULO] is being dropped, data loss could occur.
*/

IF EXISTS (select top 1 1 from [dbo].[LOG_ATIVIDADE])
    RAISERROR (N'Rows were detected. The schema update is terminating because data loss might occur.', 16, 127) WITH NOWAIT

GO
PRINT N'Rename refactoring operation with key 2256e57c-ddaa-4251-a3eb-59015f9b885d is skipped, element [dbo].[FORNECEDOR_CAMPANHAS].[Id] (SqlSimpleColumn) will not be renamed to CODIGO';


GO
PRINT N'Rename refactoring operation with key 34a29569-a7f6-4b84-9c7d-1dc58592521d is skipped, element [dbo].[FORNECEDOR_CAMPANHAS].[STATUS] (SqlSimpleColumn) will not be renamed to STATUSENVIO';


GO
PRINT N'Altering [dbo].[CAMPANHAS]...';


GO
ALTER TABLE [dbo].[CAMPANHAS] DROP COLUMN [DDD];


GO
ALTER TABLE [dbo].[CAMPANHAS]
    ADD [DDD] AS CAST ((SUBSTRING(CAST ([CELULAR] AS VARCHAR (11)), (0), (3))) AS TINYINT);


GO
PRINT N'Altering [dbo].[LOG_ATIVIDADE]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
ALTER TABLE [dbo].[LOG_ATIVIDADE] DROP COLUMN [MODULO];


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Creating [dbo].[FORNECEDOR_CAMPANHAS]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
CREATE TABLE [dbo].[FORNECEDOR_CAMPANHAS] (
    [CODIGO]       INT           IDENTITY (1, 1) NOT NULL,
    [DATAENVIAR]   SMALLDATETIME NOT NULL,
    [FORNECEDORID] INT           NOT NULL,
    [CLIENTEID]    INT           NOT NULL,
    [USUARIOID]    INT           NULL,
    [CARTEIRAID]   INT           NOT NULL,
    [ARQUIVOID]    INT           NULL,
    [QUANTIDADE]   INT           NOT NULL,
    [STATUSENVIO]  TINYINT       NOT NULL,
    PRIMARY KEY CLUSTERED ([CODIGO] ASC)
);


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
IF NOT EXISTS (SELECT OperationKey FROM [dbo].[__RefactorLog] WHERE OperationKey = '2256e57c-ddaa-4251-a3eb-59015f9b885d')
INSERT INTO [dbo].[__RefactorLog] (OperationKey) values ('2256e57c-ddaa-4251-a3eb-59015f9b885d')
IF NOT EXISTS (SELECT OperationKey FROM [dbo].[__RefactorLog] WHERE OperationKey = '34a29569-a7f6-4b84-9c7d-1dc58592521d')
INSERT INTO [dbo].[__RefactorLog] (OperationKey) values ('34a29569-a7f6-4b84-9c7d-1dc58592521d')

GO

GO
PRINT N'Update complete.';


GO
