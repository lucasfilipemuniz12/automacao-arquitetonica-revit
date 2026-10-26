// Prévia do DXF R12 gerado (LINE e TEXT) em SVG, para conferência visual sem AutoCAD.
// Uso: node apoio/dxf_para_svg.js referencia/Casa_Teste_03.dxf referencia/previa.svg
const fs = require('fs');
const [, , inp, outp] = process.argv;
const t = fs.readFileSync(inp, 'latin1').split(/\r?\n/);
const ents = []; let cur = null;
for (let i = 0; i + 1 < t.length; i += 2) {
  const c = t[i].trim(), v = t[i + 1];
  if (c === '0') { if (cur) ents.push(cur); cur = (v === 'LINE' || v === 'TEXT') ? { type: v } : null; continue; }
  if (cur) cur[c] = cur[c] === undefined ? v : cur[c];
}
const colors = { LOTE: '#000', VIAS: '#888', PASSEIO: '#aaa', CURVAS_NIVEL: '#3a3', CURVAS_MESTRAS: '#060', COTAS_TERRENO: '#b80', PAREDES_TERREO: '#c00', PAREDES_INFERIOR: '#00c', ESQUADRIAS: '#0aa', GUARDA_CORPO: '#a0a', AMBIENTES: '#333', PROJECAO: '#999', CORTE: '#c00', TERRENO_CORTE: '#3a3', NIVEIS: '#b80', TEXTO: '#000', COBERTURA: '#c60' };
let minx = 1e9, miny = 1e9, maxx = -1e9, maxy = -1e9;
const box = (x, y) => { minx = Math.min(minx, x); maxx = Math.max(maxx, x); miny = Math.min(miny, y); maxy = Math.max(maxy, y); };
for (const e of ents) { box(+e['10'], +e['20']); if (e.type === 'LINE') box(+e['11'], +e['21']); }
const pad = 3, W = maxx - minx + 2 * pad, H = maxy - miny + 2 * pad;
const X = x => (x - minx + pad).toFixed(3), Y = y => (maxy - y + pad).toFixed(3);
let s = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W.toFixed(1)} ${H.toFixed(1)}" width="${Math.round(W * 22)}" height="${Math.round(H * 22)}" style="background:#fff">`;
for (const e of ents) {
  const col = colors[e['8']] || '#000';
  if (e.type === 'LINE') s += `<line x1="${X(+e['10'])}" y1="${Y(+e['20'])}" x2="${X(+e['11'])}" y2="${Y(+e['21'])}" stroke="${col}" stroke-width="${/PAREDES|CORTE$/.test(e['8']) ? 0.06 : 0.03}"/>`;
  else { const c = e['72'] === '1'; const x = c ? +e['11'] : +e['10'], y = c ? +e['21'] : +e['20']; s += `<text x="${X(x)}" y="${Y(y)}" font-size="${(+e['40'] * 1.3).toFixed(2)}" font-family="Arial" fill="${col}"${c ? ' text-anchor="middle"' : ''}>${String(e['1']).replace(/&/g, '&amp;').replace(/</g, '&lt;')}</text>`; }
}
fs.writeFileSync(outp, s + '</svg>');
console.log('OK', outp, ents.length, 'entidades');
