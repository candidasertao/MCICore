using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    interface IDal<T>
    {
		/// <summary>
		/// Adiciona um ou mais itens nho repositório
		/// </summary>
		/// <param name="t"></param>
		/// <returns>Retorna uma Task</returns>
		Task AdicionarItensAsync(IEnumerable<T> t, int c, int? u);

		/// <summary>
		/// busca itens de acordo com o ID do registro
		/// </summary>
		/// <param name="t"> tipoo d</param>
		/// <returns></returns>
		Task<T> BuscarItemByIDAsync(T t, int? u);
		

		/// <summary>
		/// Atualizam um ou mais itens de um repositório
		/// </summary>
		/// <param name="t">tipo do dado sendo atualizado</param>
		/// <returns></returns>
		Task AtualizaItensAsync(IEnumerable<T> t, int c, int? u);



		/// <summary>
		/// Exclui um ou mais itens de um repositório
		/// </summary>
		/// <param name="t">tipo do dado</param>
		/// <returns></returns>
		Task ExcluirItensAsync(IEnumerable<T> t, int c, int? u);

		/// <summary>
		/// Caso haja algum erro na exclusão em função de chave estrangeira, um update é feito
		/// </summary>
		/// <param name="t"></param>
		/// <param name="c"></param>
		/// <param name="u"></param>
		/// <returns></returns>
		Task ExcluirItensUpdateAsync(IEnumerable<T> t, int c, int? u);

		/// <summary>
		/// Busca um ou mais itens no repositório de acordo com um critério de busca
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		Task<IEnumerable<T>> BuscarItensAsync(T t, string s, int? u);

		/// <summary>
		/// Busca um ou mais itens no repositório de acordo com o cliente e/ usuario
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		Task<IEnumerable<T>> ObterTodosAsync(T t, int? u);

		/// <summary>
		/// Obtém os dados paginados
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		Task<IEnumerable<T>> ObterTodosPaginadoAsync(T t, int? u);

	}
}
