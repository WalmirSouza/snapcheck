using Microsoft.AspNetCore.Mvc;
using SnapCheck.Data.Repositories;
using SnapCheck.Web.Models;

namespace SnapCheck.Web.Controllers;

[ApiController]
[Route("api/acessos")]
public sealed class AcessosController(IAcessoRepository acessoRepository) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginAcessoResponse>> Login(
        [FromBody] LoginAcessoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest("Email e senha são obrigatórios.");
        }

        var (resultado, usuario, token, expiraEm) = await acessoRepository.AutenticarAsync(
            request.TenantId,
            request.Email.Trim(),
            request.Senha,
            cancellationToken);

        if (resultado != LoginAcessoResultado.Sucesso || usuario is null || token is null || expiraEm is null)
        {
            return Unauthorized(new LoginAcessoResponse
            {
                Sucesso = false,
                Mensagem = resultado switch
                {
                    LoginAcessoResultado.UsuarioNaoEncontrado => "Usuário não encontrado.",
                    LoginAcessoResultado.SenhaInvalida => "Senha inválida.",
                    LoginAcessoResultado.UsuarioInativo => "Usuário inativo.",
                    _ => "Não foi possível autenticar."
                }
            });
        }

        return Ok(new LoginAcessoResponse
        {
            Sucesso = true,
            Mensagem = "Autenticação realizada com sucesso.",
            Token = token,
            ExpiraEm = expiraEm,
            Usuario = new UsuarioAcessoResponse
            {
                Id = usuario.Id,
                TenantId = usuario.TenantId,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Ativo = usuario.Ativo,
                Papeis = usuario.PapeisCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList()
            },
            Permissoes = usuario.PermissoesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        });
    }

    [HttpPost("logout")]
    public async Task<ActionResult<OperacaoResponse>> Logout(CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return BadRequest(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Token ausente."
            });
        }

        var valor = authHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(valor) || !valor.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Token inválido."
            });
        }

        var token = valor["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Token inválido."
            });
        }

        await acessoRepository.InvalidarTokenAsync(token, cancellationToken);

        return Ok(new OperacaoResponse
        {
            Sucesso = true,
            Mensagem = "Sessão encerrada com sucesso."
        });
    }

    [HttpGet("perfis-padronizados")]
    public ActionResult<IReadOnlyList<object>> ListarPerfisPadronizados()
    {
        return Ok(new object[]
        {
            new { Codigo = "super_admin", Nome = "Super Admin" },
            new { Codigo = "coordenador", Nome = "Coordenador" },
            new { Codigo = "professor", Nome = "Professor" },
            new { Codigo = "aluno", Nome = "Aluno" },
            new { Codigo = "gestor", Nome = "Gestor" },
            new { Codigo = "auditor", Nome = "Auditor" },
            new { Codigo = "operador", Nome = "Operador" }
        });
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<IReadOnlyList<UsuarioAcessoResponse>>> ListarUsuarios(
        [FromQuery] int? tenantId,
        CancellationToken cancellationToken)
    {
        var usuarios = await acessoRepository.ListarUsuariosAsync(tenantId, cancellationToken);
        return Ok(usuarios.Select(usuario => new UsuarioAcessoResponse
        {
            Id = usuario.Id,
            TenantId = usuario.TenantId,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Ativo = usuario.Ativo,
            Papeis = usuario.PapeisCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        }).ToList());
    }

    [HttpPost("usuarios")]
    public async Task<ActionResult<CriarUsuarioAcessoResponse>> CriarUsuario(
        [FromBody] CriarUsuarioAcessoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email é obrigatório.");
        }

        var (resultado, usuarioId) = await acessoRepository.CriarUsuarioAsync(
            request.TenantId,
            request.Nome.Trim(),
            request.Email.Trim(),
            string.IsNullOrWhiteSpace(request.Senha) ? null : request.Senha,
            cancellationToken);

        if (resultado == CriarUsuarioAcessoResultado.EmailJaExisteNoEscopo)
        {
            return Conflict(new CriarUsuarioAcessoResponse
            {
                Sucesso = false,
                Mensagem = "Já existe um usuário com esse email no mesmo escopo.",
                UsuarioId = null
            });
        }

        return Ok(new CriarUsuarioAcessoResponse
        {
            Sucesso = true,
            Mensagem = "Usuário criado com sucesso.",
            UsuarioId = usuarioId
        });
    }

    [HttpPost("usuarios/{usuarioId:int}/papeis/{papelId:int}")]
    public async Task<ActionResult<OperacaoResponse>> VincularPapelAoUsuario(
        [FromRoute] int usuarioId,
        [FromRoute] int papelId,
        CancellationToken cancellationToken)
    {
        var resultado = await acessoRepository.VincularPapelAoUsuarioAsync(usuarioId, papelId, cancellationToken);

        return resultado switch
        {
            VincularPapelUsuarioResultado.UsuarioNaoEncontrado => NotFound(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Usuário não encontrado."
            }),
            VincularPapelUsuarioResultado.PapelNaoEncontrado => NotFound(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Papel não encontrado."
            }),
            VincularPapelUsuarioResultado.JaVinculado => Conflict(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "O papel já está vinculado a este usuário."
            }),
            _ => Ok(new OperacaoResponse
            {
                Sucesso = true,
                Mensagem = "Papel vinculado ao usuário com sucesso."
            })
        };
    }

    [HttpGet("papeis")]
    public async Task<ActionResult<IReadOnlyList<PapelAcessoResponse>>> ListarPapeis(
        [FromQuery] int? tenantId,
        CancellationToken cancellationToken)
    {
        var papeis = await acessoRepository.ListarPapeisAsync(tenantId, cancellationToken);
        return Ok(papeis.Select(papel => new PapelAcessoResponse
        {
            Id = papel.Id,
            TenantId = papel.TenantId,
            Codigo = papel.Codigo,
            Nome = papel.Nome,
            Descricao = papel.Descricao,
            Ativo = papel.Ativo,
            Permissoes = papel.PermissoesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList()
        }).ToList());
    }

    [HttpPost("papeis")]
    public async Task<ActionResult<CriarPapelAcessoResponse>> CriarPapel(
        [FromBody] CriarPapelAcessoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            return BadRequest("Codigo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return BadRequest("Nome é obrigatório.");
        }

        var (resultado, papelId) = await acessoRepository.CriarPapelAsync(
            request.TenantId,
            request.Codigo.Trim(),
            request.Nome.Trim(),
            string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim(),
            cancellationToken);

        if (resultado == CriarPapelAcessoResultado.CodigoJaExisteNoEscopo)
        {
            return Conflict(new CriarPapelAcessoResponse
            {
                Sucesso = false,
                Mensagem = "Já existe um papel com esse código no mesmo escopo.",
                PapelId = null
            });
        }

        return Ok(new CriarPapelAcessoResponse
        {
            Sucesso = true,
            Mensagem = "Papel criado com sucesso.",
            PapelId = papelId
        });
    }

    [HttpPost("papeis/{papelId:int}/permissoes/{codigoPermissao}")]
    public async Task<ActionResult<OperacaoResponse>> ConcederPermissaoAoPapel(
        [FromRoute] int papelId,
        [FromRoute] string codigoPermissao,
        CancellationToken cancellationToken)
    {
        var resultado = await acessoRepository.ConcederPermissaoAoPapelAsync(papelId, codigoPermissao, cancellationToken);

        return resultado switch
        {
            ConcederPermissaoPapelResultado.PapelNaoEncontrado => NotFound(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Papel não encontrado."
            }),
            ConcederPermissaoPapelResultado.PermissaoNaoEncontrada => NotFound(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "Permissão não encontrada."
            }),
            ConcederPermissaoPapelResultado.JaConcedida => Conflict(new OperacaoResponse
            {
                Sucesso = false,
                Mensagem = "A permissão já está concedida a este papel."
            }),
            _ => Ok(new OperacaoResponse
            {
                Sucesso = true,
                Mensagem = "Permissão concedida ao papel com sucesso."
            })
        };
    }

    [HttpGet("permissoes")]
    public async Task<ActionResult<IReadOnlyList<PermissaoAcessoResponse>>> ListarPermissoes(CancellationToken cancellationToken)
    {
        var permissoes = await acessoRepository.ListarPermissoesAsync(cancellationToken);
        return Ok(permissoes.Select(permissao => new PermissaoAcessoResponse
        {
            Id = permissao.Id,
            Codigo = permissao.Codigo,
            Nome = permissao.Nome,
            Descricao = permissao.Descricao
        }).ToList());
    }
}