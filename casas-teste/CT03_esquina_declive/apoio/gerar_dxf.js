// Gera referencia/Casa_Teste_03.dxf (DXF R12, metros) a partir de ficha/casa.json.
// Uso: node apoio/gerar_dxf.js   (rodar na pasta Casa_Teste_03_Esquina_Declive)
// Desenhos lado a lado: implantação + terreno (origem), planta do térreo (+40 m),
// planta do inferior (+70 m), corte A-A (abaixo, y −50) e corte B-B (abaixo, x +70).
const fs = require('fs'), path = require('path');
const root = path.resolve(__dirname, '..');
const casa = JSON.parse(fs.readFileSync(path.join(root, 'ficha', 'casa.json'), 'utf8'));
const z = (x, y) => -0.18 * y - 0.025 * x + 0.003 * (x - 7.5) ** 2;

// ---- verificação dos pontos do terreno contra a fórmula
for (const [x, y, v] of casa.terreno.pontos_5m) {
  if (Math.abs(z(x, y) - v) > 0.006) throw new Error(`ponto ${x},${y}: ficha ${v} ≠ fórmula ${z(x, y).toFixed(3)}`);
}

const out = [];
const g = (code, val) => out.push(String(code), String(val));
const layers = {
  LOTE: 7, VIAS: 8, PASSEIO: 9, CURVAS_NIVEL: 3, CURVAS_MESTRAS: 94, COTAS_TERRENO: 2,
  PAREDES_TERREO: 1, PAREDES_INFERIOR: 5, ESQUADRIAS: 4, GUARDA_CORPO: 6, AMBIENTES: 7,
  PROJECAO: 8, CORTE: 1, TERRENO_CORTE: 3, NIVEIS: 2, TEXTO: 7, COBERTURA: 30
};
const line = (L, x1, y1, x2, y2) => { g(0, 'LINE'); g(8, L); g(10, x1.toFixed(4)); g(20, y1.toFixed(4)); g(30, 0); g(11, x2.toFixed(4)); g(21, y2.toFixed(4)); g(31, 0); };
const text = (L, x, y, h, s, center = false) => {
  g(0, 'TEXT'); g(8, L); g(10, x.toFixed(4)); g(20, y.toFixed(4)); g(30, 0); g(40, h); g(1, s);
  if (center) { g(72, 1); g(11, x.toFixed(4)); g(21, y.toFixed(4)); g(31, 0); }
};
const rect = (L, x1, y1, x2, y2) => { line(L, x1, y1, x2, y1); line(L, x2, y1, x2, y2); line(L, x2, y2, x1, y2); line(L, x1, y2, x1, y1); };
const fmt = v => (v >= 0 ? '+' : '') + v.toFixed(2).replace('.', ',');

// No desenho, y do projeto cresce para os fundos (sul). Para o norte ficar para cima, Y_dxf = -y.
const P = (ox, oy) => ({ L: (L, x1, y1, x2, y2) => line(L, ox + x1, oy - y1, ox + x2, oy - y2), T: (L, x, y, h, s, c) => text(L, ox + x, oy - y, h, s, c), R: (L, x1, y1, x2, y2) => rect(L, ox + x1, oy - y1, ox + x2, oy - y2) });

// ---------------- 1) IMPLANTAÇÃO + TERRENO (origem)
{
  const d = P(0, 0), lot = casa.lote;
  d.R('LOTE', 0, 0, lot.largura, lot.profundidade);
  // vias e passeios
  d.R('PASSEIO', -2, -2.5, lot.largura + 2, 0); d.R('VIAS', -2, -11.5, lot.largura + 10, -2.5);
  d.R('PASSEIO', lot.largura, -2.5, lot.largura + 2, lot.profundidade + 2); d.R('VIAS', lot.largura + 2, -11.5, lot.largura + 10, lot.profundidade + 2);
  d.T('TEXTO', lot.largura / 2, -7.5, 0.6, 'RUA DAS PAINEIRAS', true);
  d.T('TEXTO', lot.largura + 6, lot.profundidade / 2, 0.6, 'RUA DOS JACARANDAS', true);
  d.T('TEXTO', -1, lot.profundidade / 2, 0.35, 'LOTE 06', true);
  d.T('TEXTO', lot.largura / 2, lot.profundidade + 1.2, 0.35, 'LOTE 08 (VAGO)', true);
  d.T('TEXTO', 1, 1, 0.35, `LOTE 07 / QUADRA 22 - ${lot.area.toFixed(2).replace('.', ',')} m2`);
  // curvas de nível (marching squares numa malha de 0,25 m)
  const step = 0.25, nx = Math.round(lot.largura / step), ny = Math.round(lot.profundidade / step);
  const interp = (a, b, va, vb, c) => a + (b - a) * (c - va) / (vb - va);
  for (let c = 0; c >= -5.0; c -= 0.5) {
    const L = Math.abs(c % 1) < 1e-9 ? 'CURVAS_MESTRAS' : 'CURVAS_NIVEL';
    for (let i = 0; i < nx; i++) for (let j = 0; j < ny; j++) {
      const x0 = i * step, y0 = j * step, x1 = x0 + step, y1 = y0 + step;
      const v = [z(x0, y0), z(x1, y0), z(x1, y1), z(x0, y1)];
      const pts = [];
      const edges = [[0, 1, x0, y0, x1, y0], [1, 2, x1, y0, x1, y1], [2, 3, x1, y1, x0, y1], [3, 0, x0, y1, x0, y0]];
      for (const [a, b, ax, ay, bx, by] of edges) if ((v[a] - c) * (v[b] - c) < 0) pts.push([interp(ax, bx, v[a], v[b], c), interp(ay, by, v[a], v[b], c)]);
      if (pts.length === 2) d.L(L, pts[0][0], pts[0][1], pts[1][0], pts[1][1]);
    }
    // rótulo da curva mestra na divisa oeste
    if (L === 'CURVAS_MESTRAS') { const yy = (-c - 0.025 * 0.4 + 0.003 * 7.1 ** 2) / 0.18; if (yy > 0 && yy < lot.profundidade) d.T('COTAS_TERRENO', 0.4, yy - 0.15, 0.25, fmt(c)); }
  }
  for (const [x, y, v] of casa.terreno.pontos_5m) { d.L('COTAS_TERRENO', x - 0.15, y, x + 0.15, y); d.L('COTAS_TERRENO', x, y - 0.15, x, y + 0.15); d.T('COTAS_TERRENO', x + 0.2, y + 0.35, 0.2, fmt(v)); }
  // projeção da casa e lajes
  for (const l of casa.implantacao.lajes_no_terreo) d.R('PROJECAO', l.x1, l.y1, l.x2, l.y2);
  const b = casa.implantacao.corpo_principal;
  d.T('TEXTO', (b.x1 + b.x2) / 2, 8, 0.4, 'TERREO +0,15', true);
  d.T('TEXTO', (b.x1 + b.x2) / 2, 14.5, 0.3, 'TERRACO +0,15 / INFERIOR -2,85', true);
  // linhas de corte
  d.L('CORTE', 3.0, -2, 3.0, 27); d.T('CORTE', 3.0, -2.6, 0.4, 'A', true); d.T('CORTE', 3.0, 27.8, 0.4, 'A', true);
  d.L('CORTE', -2, 14.0, 17, 14.0); d.T('CORTE', -2.6, 14.2, 0.4, 'B', true); d.T('CORTE', 17.6, 14.2, 0.4, 'B', true);
  // norte
  d.L('TEXTO', -4, 2, -4, -1); d.L('TEXTO', -4, -1, -4.4, -0.2); d.L('TEXTO', -4, -1, -3.6, -0.2); d.T('TEXTO', -4, -1.6, 0.5, 'N', true);
  d.T('TEXTO', 0, -15, 0.7, 'IMPLANTACAO E TERRENO - CURVAS A CADA 0,50 m - RN 0,00 = MEIO-FIO RUA DAS PAINEIRAS', false);
  d.T('TEXTO', 0, -16.2, 0.4, `${casa.cadastro.projeto} - ${casa.cadastro.endereco} - ${casa.cadastro.bairro} - ${casa.cadastro.cidade}/${casa.cadastro.uf}`, false);
  if (casa.lote.variante_estresse) d.T('TEXTO', 0, -17.2, 0.3, 'VARIANTE (opcional): chanfro 2,50 x 2,50 m no canto NE - ver casa.json', false);
}

// ---------------- 2) e 3) PLANTAS
function planta(p, ox, titulo, nivel) {
  const d = P(ox, 0), Lw = p === 'T' ? 'PAREDES_TERREO' : 'PAREDES_INFERIOR';
  for (const w of casa.paredes.filter(w => w.p === p)) {
    const h = w.esp / 2;
    if (w.y1 === w.y2) { d.L(Lw, w.x1 - h, w.y1 - h, w.x2 + h, w.y1 - h); d.L(Lw, w.x1 - h, w.y1 + h, w.x2 + h, w.y1 + h); }
    else { d.L(Lw, w.x1 - h, w.y1 - h, w.x1 - h, w.y2 + h); d.L(Lw, w.x1 + h, w.y1 - h, w.x1 + h, w.y2 + h); }
  }
  for (const gc of (casa.guarda_corpos || []).filter(g2 => g2.p === p)) d.L('GUARDA_CORPO', gc.x1, gc.y1, gc.x2, gc.y2);
  for (const e of casa.esquadrias.filter(e => e.p === p)) {
    const [eixo, val] = e.parede.split('='), v = parseFloat(val);
    if (eixo === 'y') { d.L('ESQUADRIAS', e.de, v - 0.2, e.de, v + 0.2); d.L('ESQUADRIAS', e.ate, v - 0.2, e.ate, v + 0.2); d.L('ESQUADRIAS', e.de, v, e.ate, v); d.T('ESQUADRIAS', (e.de + e.ate) / 2, v - 0.35, 0.18, e.cod, true); }
    else { d.L('ESQUADRIAS', v - 0.2, e.de, v + 0.2, e.de); d.L('ESQUADRIAS', v - 0.2, e.ate, v + 0.2, e.ate); d.L('ESQUADRIAS', v, e.de, v, e.ate); d.T('ESQUADRIAS', v - 0.45, (e.de + e.ate) / 2, 0.18, e.cod, true); }
  }
  for (const a of casa.ambientes.filter(a => a.p === p)) {
    const area = ((a.x2 - a.x1) * (a.y2 - a.y1)).toFixed(2).replace('.', ',');
    d.T('AMBIENTES', (a.x1 + a.x2) / 2, (a.y1 + a.y2) / 2, 0.25, a.nome.toUpperCase(), true);
    d.T('AMBIENTES', (a.x1 + a.x2) / 2, (a.y1 + a.y2) / 2 + 0.4, 0.18, `${area} m2 (eixos)`, true);
    if (a.box) d.R('AMBIENTES', a.box.x1, a.box.y1, a.box.x2, a.box.y2);
  }
  if (p === 'I') { d.R('PROJECAO', 1.5, 4.0, 10.5, 9.5); d.T('PROJECAO', 6.0, 6.8, 0.25, 'SOLO / SEM AMBIENTE (SOB O TERREO)', true); }
  if (p === 'T') { d.R('COBERTURA', 0.9, 3.4, 11.1, 13.1); d.T('COBERTURA', 6.0, 3.0, 0.2, 'BEIRAL 0,60 - TELHADO 4 AGUAS 30%', true); }
  d.R('LOTE', 0, 0, 15, 25);
  d.T('TEXTO', 0, -2.5, 0.6, `${titulo} - NIVEL ${fmt(nivel)}`);
}
planta('T', 40, 'PLANTA DO TERREO', casa.niveis.terreo);
planta('I', 70, 'PLANTA DO PAVIMENTO INFERIOR', casa.niveis.inferior);

// ---------------- 4) CORTES (esquemáticos: terreno natural, pisos, lajes, paredes cortadas)
function corte(ox, oy, titulo, along, fixo, ini, fim) {
  // along: 'y' (A-A, corta em x = fixo) ou 'x' (B-B, corta em y = fixo). H = posição horizontal; V = cota.
  const L = (Lr, h1, v1, h2, v2) => line(Lr, ox + h1, oy + v1, ox + h2, oy + v2);
  const T = (Lr, h, v, s, txt, c) => text(Lr, ox + h, oy + v, s, txt, c);
  const zz = t => along === 'y' ? z(fixo, t) : z(t, fixo);
  for (let t = ini; t < fim - 1e-9; t += 0.25) L('TERRENO_CORTE', t, zz(t), t + 0.25, zz(t + 0.25));
  const n = casa.niveis;
  for (const [nome, v] of [['RN / RUA', 0], ['TERREO', n.terreo], ['INFERIOR', n.inferior], ['COBERTURA (LAJE)', n.cobertura_laje]]) {
    L('NIVEIS', ini - 3, v, ini - 1, v); T('NIVEIS', ini - 3, v === 0 ? v - 0.35 : v + 0.1, 0.22, `${nome} ${fmt(v)}`);
  }
  const b = casa.implantacao.corpo_principal;
  if (along === 'y') {
    // térreo: y 4–12.5 ; terraço 12.5–16 ; inferior 9.5–16 ; solo sob o térreo 4–9.5
    L('CORTE', 4.0, n.terreo, 16.0, n.terreo); L('CORTE', 4.0, n.terreo - 0.15, 16.0, n.terreo - 0.15);
    L('CORTE', 9.5, n.inferior, 16.0, n.inferior);
    L('CORTE', 4.0, n.cobertura_laje, 12.5, n.cobertura_laje);
    for (const yy of [4.0, 9.5, 12.5]) L('CORTE', yy, n.terreo, yy, n.cobertura_laje);
    for (const yy of [9.5, 12.5, 16.0]) L('CORTE', yy, n.inferior, yy, n.terreo - 0.15);
    L('GUARDA_CORPO', 16.0, n.terreo, 16.0, n.terreo + 1.10);
    // telhado (água sobre a frente e o fundo, 30%)
    const meio = (4.0 + 12.5) / 2; L('COBERTURA', 3.4, n.cobertura_laje, meio, n.cobertura_laje + 0.3 * (meio - 3.4)); L('COBERTURA', meio, n.cobertura_laje + 0.3 * (meio - 3.4), 13.1, n.cobertura_laje);
    T('AMBIENTES', 6.75, n.terreo + 1.2, 0.22, 'ESTAR / JANTAR', true); T('AMBIENTES', 11.0, n.terreo + 1.2, 0.22, 'COZINHA', true);
    T('AMBIENTES', 14.25, n.terreo + 0.5, 0.22, 'TERRACO', true); T('AMBIENTES', 11.0, n.inferior + 1.2, 0.22, 'BANHO', true); T('AMBIENTES', 14.25, n.inferior + 1.2, 0.22, 'SUITE', true);
    T('AMBIENTES', 6.75, n.inferior + 1.2, 0.22, 'SOLO / CONTENCAO', true);
  } else {
    L('CORTE', b.x1, n.terreo, b.x2, n.terreo); L('CORTE', b.x1, n.inferior, b.x2, n.inferior);
    for (const xx of [b.x1, 6.0, b.x2]) L('CORTE', xx, n.inferior, xx, n.terreo - 0.15);
    L('GUARDA_CORPO', b.x1, n.terreo, b.x1, n.terreo + 1.10); L('GUARDA_CORPO', b.x2, n.terreo, b.x2, n.terreo + 1.10);
    T('AMBIENTES', 3.75, n.inferior + 1.2, 0.22, 'SUITE', true); T('AMBIENTES', 8.25, n.inferior + 1.2, 0.22, 'QUARTO 02', true); T('AMBIENTES', 6.0, n.terreo + 0.5, 0.22, 'TERRACO (DESCOBERTO)', true);
    T('TEXTO', 15.0, zz(15.0) + 0.6, 0.22, 'RUA DOS JACARANDAS', true);
  }
  T('TEXTO', ini, -7.5, 0.6, titulo);
}
corte(0, -50, 'CORTE A-A (LONGITUDINAL, x = 3,00) - ESQUEMATICO', 'y', 3.0, -2, 27);
corte(40, -50, 'CORTE B-B (TRANSVERSAL, y = 14,00) - ESQUEMATICO', 'x', 14.0, -2, 17);

// ---------------- escrever DXF R12
const head = [];
const h = (c, v) => head.push(String(c), String(v));
h(0, 'SECTION'); h(2, 'HEADER'); h(9, '$ACADVER'); h(1, 'AC1009'); h(9, '$INSUNITS'); h(70, 6); h(0, 'ENDSEC');
h(0, 'SECTION'); h(2, 'TABLES'); h(0, 'TABLE'); h(2, 'LAYER'); h(70, Object.keys(layers).length);
for (const [n, c] of Object.entries(layers)) { h(0, 'LAYER'); h(2, n); h(70, 0); h(62, c); h(6, 'CONTINUOUS'); }
h(0, 'ENDTAB'); h(0, 'ENDSEC'); h(0, 'SECTION'); h(2, 'ENTITIES');
const tail = ['0', 'ENDSEC', '0', 'EOF'];
const file = path.join(root, 'referencia', 'Casa_Teste_03.dxf');
fs.mkdirSync(path.dirname(file), { recursive: true });
// DXF R12 é ANSI: troca acentos para não depender de codepage.
const ascii = s => s.replace(/[\u2014\u2013]/g, '-').replace(/\u2192/g, '->').replace(/\u00b2/g, '2').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/[^\x00-\x7F]/g, '?');
fs.writeFileSync(file, ascii([...head, ...out, ...tail].join('\r\n') + '\r\n'), 'latin1');
console.log(`OK: ${file} (${(fs.statSync(file).size / 1024).toFixed(0)} KB, ${out.length / 2} grupos)`);
