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
PRINT N'Dropping [dbo].[FK_LAYOUT_VARIAVEIS_ToTable]...';


GO
ALTER TABLE [dbo].[LAYOUT_VARIAVEIS] DROP CONSTRAINT [FK_LAYOUT_VARIAVEIS_ToTable];


GO
PRINT N'Altering [dbo].[CAMPANHAS]...';


GO
ALTER TABLE [dbo].[CAMPANHAS] DROP COLUMN [DDD];


GO
ALTER TABLE [dbo].[CAMPANHAS]
    ADD [DDD] AS CAST ((SUBSTRING(CAST ([CELULAR] AS VARCHAR (11)), (0), (3))) AS TINYINT);


GO
PRINT N'Starting rebuilding table [dbo].[LAYOUT_VARIAVEIS]...';


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER OFF;


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [dbo].[tmp_ms_xx_LAYOUT_VARIAVEIS] (
    [CODIGO]    INT           IDENTITY (1, 1) NOT NULL,
    [LEIAUTEID] INT           NOT NULL,
    [VARIAVEL]  VARCHAR (150) NOT NULL,
    [IDCOLUNA]  SMALLINT      NOT NULL,
    PRIMARY KEY CLUSTERED ([CODIGO] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [dbo].[LAYOUT_VARIAVEIS])
    BEGIN
        SET IDENTITY_INSERT [dbo].[tmp_ms_xx_LAYOUT_VARIAVEIS] ON;
        INSERT INTO [dbo].[tmp_ms_xx_LAYOUT_VARIAVEIS] ([CODIGO], [LEIAUTEID], [VARIAVEL], [IDCOLUNA])
        SELECT   [CODIGO],
                 [LEIAUTEID],
                 [VARIAVEL],
                 [IDCOLUNA]
        FROM     [dbo].[LAYOUT_VARIAVEIS]
        ORDER BY [CODIGO] ASC;
        SET IDENTITY_INSERT [dbo].[tmp_ms_xx_LAYOUT_VARIAVEIS] OFF;
    END

DROP TABLE [dbo].[LAYOUT_VARIAVEIS];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_LAYOUT_VARIAVEIS]', N'LAYOUT_VARIAVEIS';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
SET ANSI_NULLS, QUOTED_IDENTIFIER ON;


GO
PRINT N'Creating [dbo].[LAYOUT_VARIAVEIS].[IX_VARIAVEL_LAYOUT]...';


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_VARIAVEL_LAYOUT]
    ON [dbo].[LAYOUT_VARIAVEIS]([LEIAUTEID] ASC, [VARIAVEL] ASC, [IDCOLUNA] ASC);


GO
PRINT N'Creating [dbo].[FK_LAYOUT_VARIAVEIS_ToTable]...';


GO
ALTER TABLE [dbo].[LAYOUT_VARIAVEIS] WITH NOCHECK
    ADD CONSTRAINT [FK_LAYOUT_VARIAVEIS_ToTable] FOREIGN KEY ([LEIAUTEID]) REFERENCES [dbo].[LAYOUT] ([LEIAUTEID]);


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
PRINT N'Checking existing data against newly created constraints';


GO
USE [$(DatabaseName)];


GO
ALTER TABLE [dbo].[LAYOUT_VARIAVEIS] WITH CHECK CHECK CONSTRAINT [FK_LAYOUT_VARIAVEIS_ToTable];


GO
PRINT N'Update complete.';


GO
