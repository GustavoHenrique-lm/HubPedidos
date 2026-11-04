
using HubPedidos.Api.Authentication;
using HubPedidos.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HubPedidos.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Registrar(
        [FromBody] RegistroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest("E-mail e senha são obrigatórios.");
        }
        var usuarioExistente = await _userManager.FindByEmailAsync(
            request.Email).ConfigureAwait(false);

        if (usuarioExistente is not null)
        {
            return Conflict("Este e-mail já está cadastrado.");
        }
        var usuario = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };
        var resultado = await _userManager.CreateAsync(
            usuario,
            request.Senha).ConfigureAwait(false);
        if (!resultado.Succeeded)
        {
            return BadRequest(new
            {
                Erros = resultado.Errors.Select(e => e.Description)
            });
        }
        return Ok(new
        {
            Mensagem = "Usuário cadastrado com sucesso.",
            UsuarioId = usuario.Id
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var usuario = await _userManager.FindByEmailAsync(
            request.Email).ConfigureAwait(false);

        if (usuario is null)
        {
            return Unauthorized("E-mail ou senha inválidos.");
        }

        var senhaValida = await _userManager.CheckPasswordAsync(
            usuario,
            request.Senha).ConfigureAwait(false);

        if (!senhaValida)
        {
            return Unauthorized("E-mail ou senha inválidos.");
        }

        var token = _tokenService.GerarToken(usuario);

        return Ok(new
        {
            Token = token,
            UsuarioId = usuario.Id,
            Email = usuario.Email
        });
    }
}

public record RegistroRequest(string Email, string Senha);

public record LoginRequest(string Email, string Senha);
