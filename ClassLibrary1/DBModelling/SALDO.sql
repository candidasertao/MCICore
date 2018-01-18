--CREATE RULE [dbo].[SALDO] AS @saldo >=0

--GO
--exec sp_bindrule 'SALDO','USUARIOS.SALDO'
--GO
--exec sp_bindrule 'SALDO','CLIENTES.SALDO'


----EXEC sp_unbindrule 'USUARIOS.SALDO'


--DROP RULE [SALDO] --as @saldo

