# Decisões Técnicas – Hub de Pedidos

## Registro explícito x reflexão

O registro explícito permite cadastrar manualmente cada processador,
sendo simples e direto, mas exige alterações no código sempre que
um novo processador é criado.

A reflexão permite descobrir automaticamente os processadores
por meio de atributos personalizados. Neste projeto, foi escolhida
essa abordagem para facilitar a identificação dos processadores.

O catálogo examina somente a assembly informada, verifica os
metadados, exige construtor sem parâmetros e impede categorias
duplicadas.

## Escolhas do projeto

- Records para representar contratos e itens imutáveis.
- Validação na criação dos objetos do domínio.
- Switch expression para classificar pedidos.
- Reflection para descobrir processadores.
- xUnit para testes automatizados.
- Nenhum banco de dados, pois o escopo não exige persistência.
