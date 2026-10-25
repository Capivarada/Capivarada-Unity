# Pontos, inventário e loja de skins no PlayFab Legacy

O projeto usa CloudScript v1 e User ReadOnly Data para manter o saldo e o inventário sob controle do servidor. A implementação local já chama as funções `GetPlayerEconomy`, `PurchaseSkinBox`, `GetPurchaseResult`, `EquipSkin` e `ProcessMatchResult`.

## Recompensas de partida

O CloudScript concede 10 pontos por partida, mais 15 pontos se houver vitória, e 150 pontos por nível conquistado. O saldo é salvo em `Points`. XP e nível usam `CurrentXP` e `Level`; as skins pertencentes ao jogador ficam em `OwnedSkinIds` e a escolhida em `EquippedSkinId`.

## Configuração no PlayFab

1. No Game Manager, abra **Automation > CloudScript (Legacy)** e publique o conteúdo de `Assets/MyProject/CloudScript/CapivaradaCloudScript.txt`.
2. Em **Content > Title Data**, crie a chave interna `CapivaradaEconomyConfig` e cole o JSON exportado em `Assets/MyProject/CloudScript/CapivaradaEconomyConfig.json`.
3. Em cada `WardrobeBox`, preencha somente preço, raridade visual e as chances por raridade. As skins possíveis ficam centralizadas no `SkinCatalog`, classificadas pela raridade; chances com zero não aparecem na janela de detalhes.
4. Use IDs estáveis em camelCase: por exemplo, `caixaComum`, `caixaRara`, `padrao`, `azul` e `prata`. Os mesmos IDs devem ser publicados na configuração do CloudScript.
4. No Unity, execute **Capivarada > Export PlayFab Economy Config** sempre que mudar uma caixa ou sua lista de skins; depois substitua o valor da chave interna no PlayFab pelo JSON gerado.
5. Para os pacotes reais de pontos, crie os produtos e preços na área de monetização da plataforma escolhida. Em cada `PointPackage`, registre o identificador externo no campo `Future PlayFab Product Id`. A confirmação de pagamento deve chamar uma função de servidor que credite `Points`; nunca aumente o saldo somente pelo cliente.

## Limites atuais

O painel visual de pontos já fica separado da loja de caixas, mas a cobrança real depende da plataforma de pagamento e da configuração do título PlayFab. Por isso, nenhum produto ou cobrança externa é criado automaticamente pelo projeto.
