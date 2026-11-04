using System.Reflection;
using HubPedidos.Domain.Enums;

namespace HubPedidos.Application.Processors;

public class CatalogoProcessadores
{
    private readonly Dictionary<CategoriaPedido, IProcessadorPedido> _processadores;

    public CatalogoProcessadores(Assembly assembly)
    {
        _processadores = new();

        var tipos = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract &&
                        typeof(IProcessadorPedido).IsAssignableFrom(t));

        foreach (var tipo in tipos)
        {
            var atributo = tipo.GetCustomAttribute<ProcessadorPedidoAttribute>();

            if (atributo is null || atributo.Versao <= 0)
            {
                throw new InvalidOperationException(
                    $"Metadados inválidos no processador {tipo.Name}.");
            }

            if (tipo.GetConstructor(Type.EmptyTypes) is null)
            {
                throw new InvalidOperationException(
                    $"O processador {tipo.Name} precisa de construtor sem parâmetros.");
            }

            var processador = (IProcessadorPedido)Activator.CreateInstance(tipo)!;

            if (!_processadores.TryAdd(atributo.Categoria, processador))
            {
                throw new InvalidOperationException(
                    $"Já existe processador para {atributo.Categoria}.");
            }
        }
    }

    public IProcessadorPedido Obter(CategoriaPedido categoria)
    {
        if (!_processadores.TryGetValue(categoria, out var processador))
        {
            throw new InvalidOperationException(
                $"Nenhum processador encontrado para {categoria}.");
        }

        return processador;
    }
}
