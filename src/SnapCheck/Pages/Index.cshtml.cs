using Microsoft.AspNetCore.Mvc.RazorPages;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Web.Pages;

public class IndexModel(
    IConfiguracaoRepository configuracaoRepository,
    IDbConnectionFactory connectionFactory) : PageModel
{
    public async Task OnGetAsync()
    {
        try
        {
            var connectionString = await configuracaoRepository.ObterValorAsync(ConfigKeys.PostgresConnectionString);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                connectionFactory.SetConnectionString(connectionString);
            }
        }
        catch
        {
            // Banco ainda não configurado — a página carrega normalmente.
        }
    }
}
