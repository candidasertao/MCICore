using Dapper;
using Helpers;
using Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class DALLogAtividade : IDal<LogAtividadeModel>
    {
        public async Task AdicionarItensAsync<T>(T _new, T _old, int ClienteID, int? UsuarioID, ModuloAtividadeEnumns modulo, TiposLogAtividadeEnums tipo)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                SqlTransaction tran = conn.BeginTransaction();

                var t = GetLog(_new, _old);

                try
                {
                    var hoje = DateTime.Now;

                    await conn.ExecuteAsync(@"INSERT INTO LOG_ATIVIDADE (CLIENTEID, USUARIOID, DATA, DESCRICAO, TIPOATIVIDADE, DATADIA, MODULO, CARTEIRAID) VALUES (@ClienteID, @UsuarioID, @Data, @Descricao, @TipoAtividade, @DataDia, @Modulo, @CarteiraID)", t.Select(a => new
                    {
                        ClienteID = ClienteID,
                        UsuarioID = UsuarioID,
                        Data = hoje,
                        Descricao = a.Descricao,
                        TipoAtividade = (byte)tipo,
                        DataDia = hoje.Date,
                        Modulo = (byte)modulo,
                        CarteiraID = a.Carteira != null ? a.Carteira.CarteiraID : null
                    }), transaction: tran, commandTimeout: 888);

                    tran.Commit();
                }
                catch (Exception err)
                {
                    tran.Rollback();
                    throw err;
                }
                finally
                {
                    tran.Dispose();
                    conn.Close();
                }
            }
        }

        IEnumerable<LogAtividadeModel> GetLog<T>(T _new, T _old)
        {
            if (_new == null)
                return null;

            var tipo = _new.GetType();

            if (tipo == typeof(List<BlackListModel>))
            {
                var l = _new as IEnumerable<BlackListModel>;

                return (new LogAtividadeModel[]
                {
                    new LogAtividadeModel
                    {
                        Descricao = string.Format("{0} Celular(es)", l.Count())
                    }
                });
            }
            else if (tipo == typeof(List<TipoCampanhaModel>))
            {
                var l = _new as IEnumerable<TipoCampanhaModel>;

                return (from o in l
                        select new LogAtividadeModel
                        {
                            Descricao = string.Format("{0}", o.TipoCampanha)
                        });
            }
            else if (tipo == typeof(List<PadraoPostagensModel>) || tipo == typeof(PadraoPostagensModel))
            {
                if (tipo == typeof(List<PadraoPostagensModel>))
                {
                    var l = _new as IEnumerable<PadraoPostagensModel>;

                    return (from o in l
                            select new LogAtividadeModel
                            {
                                Descricao = string.Format("{0}", o.Padrao),
                                Carteira = new CarteiraModel { CarteiraID = o.Carteira.CarteiraID }
                            });
                }
                else
                {
                    var l = _new as PadraoPostagensModel;

                    return (new LogAtividadeModel[]
                            {
                                new LogAtividadeModel
                                {
                                    Descricao = string.Format("{0}", l.Padrao),
                                    Carteira = new CarteiraModel { CarteiraID = l.Carteira.CarteiraID }
                                }
                            });
                }
            }
            else if (tipo == typeof(List<CarteiraModel>))
            {
                var l = _new as IEnumerable<CarteiraModel>;

                return (from o in l
                        select new LogAtividadeModel
                        {
                            Descricao = string.Format("{0}", o.Carteira),
                            Carteira = new CarteiraModel { CarteiraID = o.CarteiraID }
                        });
            }
            else if (tipo == typeof(List<GrupoUsuariosModel>) || tipo == typeof(GrupoUsuariosModel))
            {

                if (tipo == typeof(List<GrupoUsuariosModel>))
                {
                    var l = _new as IEnumerable<GrupoUsuariosModel>;

                    return (from o in l
                            select new LogAtividadeModel
                            {
                                Descricao = string.Format("{0}", o.Nome)
                            });
                }
                else
                {
                    var l = _new as GrupoUsuariosModel;

                    return (new LogAtividadeModel[]
                            {
                                new LogAtividadeModel
                                {
                                    Descricao = string.Format("Saldo de {0}", l.Nome)
                                }
                            });
                }
            }
            else if (tipo == typeof(List<LeiauteModel>) || tipo == typeof(LeiauteModel))
            {

                if (tipo == typeof(List<LeiauteModel>))
                {
                    var l = _new as IEnumerable<LeiauteModel>;

                    return (from o in l
                            select new LogAtividadeModel
                            {
                                Descricao = string.Format("{0}", o.Nome)
                            });
                }
                else
                {
                    var l = _new as LeiauteModel;

                    return (new LogAtividadeModel[]
                            {
                                new LogAtividadeModel
                                {
                                    Descricao = string.Format("{0} definido como padrao", l.Nome)
                                }
                            });
                }
            }
            else if (tipo == typeof(List<UsuarioModel>) || tipo == typeof(UsuarioModel))
            {
                if (tipo == typeof(List<UsuarioModel>))
                {
                    var l = _new as IEnumerable<UsuarioModel>;

                    return (from o in l
                            select new LogAtividadeModel
                            {
                                Descricao = string.Format("{0}", o.Nome)
                            });
                }
                else
                {
                    var l = _new as UsuarioModel;

                    return (new LogAtividadeModel[]
                            {
                                new LogAtividadeModel
                                {
                                    Descricao = string.Format("{0}", l.Nome)
                                }
                            });
                }
            }
            else if (tipo == typeof(List<GestorModel>))
            {
                var l = _new as IEnumerable<GestorModel>;

                return (from o in l
                        select new LogAtividadeModel
                        {
                            Descricao = string.Format("{0}", o.Nome)
                        });

            }
            else if (tipo == typeof(List<SegmentacaoModel>) || tipo == typeof(SegmentacaoModel))
            {
                if (tipo == typeof(List<SegmentacaoModel>))
                {
                    var l = _new as IEnumerable<SegmentacaoModel>;

                    return (from o in l
                            select new LogAtividadeModel
                            {
                                Descricao = string.Format("{0}", o.Nome)
                            });
                }
                else
                {
                    var l = _new as SegmentacaoModel;

                    return (new LogAtividadeModel[]
                            {
                                new LogAtividadeModel
                                {
                                    Descricao = string.Format("{0}", l.Nome)
                                }
                            });
                }
            }            
            else
                return null;
        }

        public Task AtualizaItensAsync(IEnumerable<LogAtividadeModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<LogAtividadeModel> BuscarItemByIDAsync(LogAtividadeModel t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LogAtividadeModel>> BuscarItensAsync(LogAtividadeModel t, string s, int? u)
        {
            throw new NotImplementedException();
        }

        public Task ExcluirItensAsync(IEnumerable<LogAtividadeModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LogAtividadeModel>> ObterTodosAsync(LogAtividadeModel t, int? u)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<LogAtividadeModel>> DashBoard(int c, int? u)
        {
            using (var conn = new SqlConnection(Util.ConnString))
            {
                await conn.OpenAsync();
                try
                {
                    string query = @"SELECT TOP 30 L.DATA
	                                    ,L.DESCRICAO
	                                    ,L.TIPOATIVIDADE
	                                    ,L.MODULO
	                                    ,U.NOME USUARIO
                                        ,C.CARTEIRA
                                        ,S.NOME AS CLIENTE
                                    FROM [dbo].[LOG_ATIVIDADE] L
                                    INNER JOIN [dbo].[CLIENTES] S ON S.CLIENTEID = L.CLIENTEID
                                    LEFT JOIN [dbo].[CARTEIRAS] C ON C.CARTEIRAID = L.CARTEIRAID
                                    LEFT JOIN [dbo].[USUARIOS] U ON U.USUARIOID = L.USUARIOID
                                    LEFT JOIN [dbo].[USUARIOS_CARTEIRA] UC ON UC.CARTEIRAID = L.CARTEIRAID	
                                        AND UC.USUARIOID = U.USUARIOID
                                    WHERE L.CLIENTEID = @CLIENTEID";

                    var p = new DynamicParameters();
                    p.Add("ClienteID", c, DbType.Int32, ParameterDirection.Input);
                    p.Add("DataDia", DateTime.Now.Date, DbType.Date, ParameterDirection.Input);

                    if (u.HasValue)
                    {
                        p.Add("UsuarioID", u.Value, DbType.Int32, ParameterDirection.Input);
                        query += @" AND (L.USUARIOID = @UsuarioID OR UC.USUARIOID = @UsuarioID)";
                    }

                    query += @" ORDER BY DATA DESC ";

                    var result = await conn.QueryAsync<dynamic>(query, p);

                    if (result != null)
                    {
                        return result.Select(a => new LogAtividadeModel()
                        {
                            Cliente = new ClienteModel() { ClienteID = c, Nome = a.CLIENTE },
                            Carteira = new CarteiraModel() { Carteira = a.CARTEIRA },
                            Usuario = new UsuarioModel() { Nome = a.USUARIO },
                            Data = a.DATA,
                            Descricao = a.DESCRICAO,
                            Tipo = ((TiposLogAtividadeEnums)Enum.Parse(typeof(TiposLogAtividadeEnums), a.TIPOATIVIDADE.ToString())),
                            Modulo = ((ModuloAtividadeEnumns)Enum.Parse(typeof(ModuloAtividadeEnumns), a.MODULO.ToString()))
                        });
                    }
                    else
                        return null;

                }
                catch (Exception err)
                {
                    throw err;
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public Task ExcluirItensUpdateAsync(IEnumerable<LogAtividadeModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<LogAtividadeModel>> ObterTodosPaginadoAsync(LogAtividadeModel t, int? u)
        {
            throw new NotImplementedException();
        }

        public Task AdicionarItensAsync(IEnumerable<LogAtividadeModel> t, int c, int? u)
        {
            throw new NotImplementedException();
        }
    }
}
