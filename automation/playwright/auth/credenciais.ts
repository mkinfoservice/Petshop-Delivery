/**
 * Credenciais e configurações de ambiente.
 *
 * Para usar credenciais diferentes por ambiente, defina as variáveis:
 *   VENDAPPS_USER=outro-usuario
 *   VENDAPPS_PASS=outra-senha
 *   VENDAPPS_URL=https://outraempresa.vendapps.com.br
 */
export const CREDENCIAIS = {
  usuario: process.env.VENDAPPS_USER ?? "mayk",
  senha: process.env.VENDAPPS_PASS ?? "Raxp43fl37pla@",
  usuarioMotoboy: process.env.VENDAPPS_USER_MOTOBOY ?? "21992329239",
  senhaMotoboy: process.env.VENDAPPS_PASS_MOTOBOY ?? "0101",
  /** URL do frontend (SPA) */
  baseURL: process.env.VENDAPPS_URL ?? "https://novaempresa.vendapps.com.br",
  /** URL da API REST (backend) */
  apiURL: process.env.VENDAPPS_API_URL ?? "https://vendapps.onrender.com",
  /** Chave do token JWT no localStorage */
  tokenKey: "petshop_admin_token",
} as const;
