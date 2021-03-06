﻿CREATE PROC dbo.CAMPANHA_CONSOLIDADO_ENVIOS_PAGINACAO --30,1, '2017-4-26',  '2017-4-29', 1,NULL,0,0
@PaginaSize		INT,
@PaginaAtual	INT,
@DataInicial	DATE,
@DataFinal		DATE,
@ClienteID		INT,
@UsuarioID		INT,
@Paginas		INT OUTPUT,
@Registros		INT OUTPUT
AS
BEGIN
DECLARE @TMP TABLE
(
DATAENVIAR		SMALLDATETIME,
ENVIADA			INT,
EXCLUIDA		INT,
ERRO			INT,
SUSPENSA		INT,
ENTREGUE		INT,
EXPIRADA		INT,
DATADIA			DATE,
FORNECEDORID	INT,
CANCELADA		INT,
USUARIOID		INT,
REGISTROS		INT
);

WITH CONSOLIDADO AS
(
	SELECT DATAENVIAR, ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, FORNECEDORID, CANCELADA, USUARIOID, ROW_NUMBER() OVER ( ORDER BY CODIGO ) REGISTRO FROM CAMPANHAS_CONSOLIDADO WHERE CLIENTEID=@ClienteID AND DATADIA BETWEEN @DataInicial AND @DataFinal
)
INSERT @TMP
SELECT DATAENVIAR, ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, FORNECEDORID, CANCELADA, USUARIOID, REGISTRO FROM CONSOLIDADO ORDER BY DATAENVIAR
	IF(@UsuarioID IS NULL)
		SELECT DATAENVIAR, ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, FORNECEDORID, CANCELADA, USUARIOID FROM @TMP T
		WHERE T.REGISTROS BETWEEN (@PAGINAATUAL-1)*@PAGINASIZE AND ((@PAGINAATUAL-1)*@PAGINASIZE)+@PAGINASIZE
	ELSE
		SELECT DATAENVIAR, ENVIADA, EXCLUIDA, ERRO, SUSPENSA, ENTREGUE, EXPIRADA, DATADIA, FORNECEDORID, CANCELADA, USUARIOID FROM @TMP T
		WHERE T.REGISTROS BETWEEN (@PAGINAATUAL-1)*@PAGINASIZE AND ((@PAGINAATUAL-1)*@PAGINASIZE)+@PAGINASIZE AND USUARIOID=@UsuarioID
END