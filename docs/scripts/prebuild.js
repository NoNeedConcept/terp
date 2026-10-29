// Generates API reference Markdown for VitePress from .NET XML documentation files.
import { mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { glob } from 'glob'
import { parseStringPromise } from 'xml2js'

const XML_PATHS = [
  '../src/Terp/bin/*/*/Terp.xml',
  '../src/Terp.Akka/bin/*/*/Terp.Akka.xml'
]
const OUTPUT_DIR = 'api'
const EXCLUDED_NAMESPACES = ['System', 'Microsoft', 'Internal']
const EXCLUDED_TYPES = ['Terp.Akka.Entities.EntityRegionMetrics']

const DOCS_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const OUT = join(DOCS_ROOT, OUTPUT_DIR)

/** @type {Map<string, any>} full type name -> type model */
const types = new Map()

// ---------------------------------------------------------------- helpers

const children = (node) => node?.$$ ?? []
const childrenNamed = (node, name) => children(node).filter((c) => c['#name'] === name)
const firstNamed = (node, name) => childrenNamed(node, name)[0]

const sanitize = (name) => name.replace(/`/g, '').replace(/[<>:"/\\|?*]/g, '_')
const typeFile = (fullName) => sanitize(fullName)
const nsFile = (ns) => `${sanitize(ns)}.Namespace`

const isExcluded = (ns) => EXCLUDED_NAMESPACES.some((x) => ns === x || ns.startsWith(`${x}.`))

const escapeText = (s) =>
  s.replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/\{/g, '&#123;').replace(/\}/g, '&#125;')

/** Converts a documentation-ID type (System.Collections.Generic.List{System.String}) to a readable name. */
function shortType(id) {
  return id
    .replace(/\b(?:\w+\.)+(?=\w)/g, '')
    .replace(/(\w)`+\d+/g, '$1')
    .replace(/`+\d+/g, 'T')
    .replace(/@/g, '')
    .replace(/\{/g, '<')
    .replace(/\}/g, '>')
    .replace(/,/g, ', ')
}

/** Splits at top-level commas (ignores commas inside {}, (), []). */
function splitTopLevel(s) {
  const parts = []
  let depth = 0
  let cur = ''
  for (const ch of s) {
    if ('{(['.includes(ch)) depth++
    if ('})]'.includes(ch)) depth--
    if (ch === ',' && depth === 0) {
      parts.push(cur)
      cur = ''
    } else {
      cur += ch
    }
  }
  if (cur) parts.push(cur)
  return parts
}

/** Finds the declaring type of a member ID body (without prefix and parameters). */
function ownerOf(path) {
  let cur = path
  while (cur.includes('.')) {
    cur = cur.slice(0, cur.lastIndexOf('.'))
    if (types.has(cur)) return cur
  }
  return undefined
}

function namespaceOf(fullName) {
  let cur = fullName
  while (cur.includes('.')) {
    cur = cur.slice(0, cur.lastIndexOf('.'))
    if (!types.has(cur)) return cur
  }
  return ''
}

// ------------------------------------------------------- XML -> Markdown

function typeLink(fullName, label) {
  const code = `\`${label}\``
  return types.has(fullName) ? `[${code}](./${typeFile(fullName)})` : code
}

function crefToMarkdown(cref) {
  if (!cref || cref.length < 3) return ''
  const kind = cref[0]
  const id = cref.slice(2)
  if (kind === 'T') return typeLink(id, shortType(id))
  const path = id.split('(')[0]
  const owner = ownerOf(path)
  const member = path.slice(path.lastIndexOf('.') + 1).replace('#ctor', owner ? shortType(owner) : 'ctor')
  const label = owner ? `${shortType(owner)}.${shortType(member)}` : shortType(path)
  return owner ? typeLink(owner, label) : `\`${label}\``
}

function dedent(text) {
  const lines = text.replace(/^\r?\n/, '').replace(/\s+$/, '').split(/\r?\n/)
  const indent = Math.min(...lines.filter((l) => l.trim()).map((l) => l.match(/^\s*/)[0].length), Infinity)
  return lines.map((l) => l.slice(Number.isFinite(indent) ? indent : 0)).join('\n')
}

const rawText = (node) =>
  children(node).map((c) => (c['#name'] === '__text__' ? c._ ?? '' : rawText(c))).join('')

/** Renders a list of XML child nodes to Markdown. Fenced code is stashed and restored afterwards. */
function renderNodes(nodes, blocks) {
  return nodes.map((n) => renderNode(n, blocks)).join('')
}

function renderNode(node, blocks) {
  const inner = () => renderNodes(children(node), blocks)
  switch (node['#name']) {
    case '__text__':
      return escapeText((node._ ?? '').replace(/\s+/g, ' '))
    case 'see': {
      const { cref, langword, href } = node.$ ?? {}
      if (cref) return crefToMarkdown(cref)
      if (langword) return `\`${langword}\``
      if (href) return `[${inner().trim() || href}](${href})`
      return inner()
    }
    case 'seealso':
    case 'inheritdoc':
      return ''
    case 'paramref':
    case 'typeparamref':
      return `\`${node.$?.name ?? ''}\``
    case 'c':
      return `\`${rawText(node).trim()}\``
    case 'code': {
      blocks.push('```csharp\n' + dedent(rawText(node)) + '\n```')
      return `\n\n\u0000${blocks.length - 1}\u0000\n\n`
    }
    case 'para':
      return `\n\n${inner()}\n\n`
    case 'list':
      return `\n\n${renderList(node, blocks)}\n\n`
    default:
      return inner()
  }
}

function renderList(node, blocks) {
  const kind = node.$?.type ?? 'bullet'
  const items = childrenNamed(node, 'item')
  const part = (item, name) => {
    const el = firstNamed(item, name)
    return el ? finalize(renderNodes(children(el), blocks), blocks).replace(/\n+/g, ' ') : ''
  }
  if (kind === 'table') {
    const rows = items.map((i) => `| ${part(i, 'term')} | ${part(i, 'description')} |`)
    return ['| Term | Description |', '| --- | --- |', ...rows].join('\n')
  }
  return items
    .map((item, i) => {
      const term = part(item, 'term')
      const desc = part(item, 'description')
      const text = term && desc ? `**${term}**: ${desc}` : term || desc || finalize(renderNodes(children(item), blocks), blocks)
      return `${kind === 'number' ? `${i + 1}.` : '-'} ${text}`
    })
    .join('\n')
}

function finalize(text, blocks) {
  return text
    .split('\n\n')
    .map((s) => s.trim())
    .filter(Boolean)
    .join('\n\n')
    .replace(/\u0000(\d+)\u0000/g, (_, i) => blocks[Number(i)])
}

function markdownOf(node) {
  if (!node) return ''
  const blocks = []
  return finalize(renderNodes(children(node), blocks), blocks)
}

// ----------------------------------------------------------- model build

async function loadMembers() {
  const files = await glob(XML_PATHS, { cwd: DOCS_ROOT, absolute: true, windowsPathsNoEscape: true })
  if (files.length === 0) {
    throw new Error(`No XML documentation files found for: ${XML_PATHS.join(', ')}. Run "dotnet build src/Terp.slnx" first.`)
  }
  const members = []
  for (const file of files) {
    const parsed = await parseStringPromise(readFileSync(file, 'utf8'), {
      explicitChildren: true,
      preserveChildrenOrder: true,
      charsAsChildren: true,
      includeWhiteChars: true
    })
    const membersNode = firstNamed(parsed.doc, 'members')
    members.push(...childrenNamed(membersNode, 'member'))
    console.log(`Parsed ${file}`)
  }
  return members
}

function buildModel(members) {
  const byKind = (k) => members.filter((m) => m.$.name.startsWith(`${k}:`))

  for (const m of byKind('T')) {
    const fullName = m.$.name.slice(2)
    types.set(fullName, { fullName, name: shortType(fullName), node: m, ctors: [], fields: [], properties: [], events: [], methods: [] })
  }
  for (const [fullName, t] of types) t.namespace = namespaceOf(fullName)
  for (const [fullName, t] of types) {
    if (isExcluded(t.namespace) || !t.namespace || EXCLUDED_TYPES.includes(fullName)) types.delete(fullName)
  }

  const targets = { F: 'fields', P: 'properties', E: 'events', M: 'methods' }
  for (const m of members) {
    const kind = m.$.name[0]
    if (!targets[kind]) continue
    const id = m.$.name.slice(2)
    const path = id.split('(')[0]
    const owner = ownerOf(path)
    if (!owner || !types.has(owner)) continue
    const isCtor = kind === 'M' && /\.#c?ctor/.test(path)
    const rawName = path.slice(path.lastIndexOf('.') + 1)
    const paramTypes = id.includes('(') ? splitTopLevel(id.slice(id.indexOf('(') + 1, id.lastIndexOf(')'))) : []
    const type = types.get(owner)
    const entry = { id, node: m, name: isCtor ? type.name : shortType(rawName), paramTypes, isCtor }
    type[isCtor ? 'ctors' : targets[kind]].push(entry)
  }
}

// -------------------------------------------------------------- rendering

function signature(type, member) {
  const names = childrenNamed(member.node, 'param').map((p) => p.$.name)
  const params = member.paramTypes.map((t, i) => `${shortType(t)}${names[i] ? ` ${names[i]}` : ''}`)
  return `${member.name}(${params.join(', ')})`
}

function docSections(node, level) {
  const h = '#'.repeat(level)
  const out = []
  const params = childrenNamed(node, 'param')
  if (params.length) {
    out.push(`${h} Parameters`, params.map((p) => `- \`${p.$.name}\`: ${markdownOf(p)}`).join('\n'))
  }
  const typeParams = childrenNamed(node, 'typeparam')
  if (typeParams.length) {
    out.push(`${h} Type parameters`, typeParams.map((p) => `- \`${p.$.name}\`: ${markdownOf(p)}`).join('\n'))
  }
  const returns = markdownOf(firstNamed(node, 'returns'))
  if (returns) out.push(`${h} Returns`, returns)
  const exceptions = childrenNamed(node, 'exception')
  if (exceptions.length) {
    out.push(`${h} Exceptions`, exceptions.map((e) => `- ${crefToMarkdown(e.$?.cref)}: ${markdownOf(e)}`).join('\n'))
  }
  for (const ex of childrenNamed(node, 'example')) out.push(`${h} Example`, markdownOf(ex))
  return out
}

function memberSection(title, type, list, withSignature) {
  if (list.length === 0) return []
  const out = [`## ${title}`]
  for (const m of list) {
    out.push(`### \`${m.name}\``)
    if (withSignature) out.push('```csharp\n' + signature(type, m) + '\n```')
    const summary = markdownOf(firstNamed(m.node, 'summary')) || markdownOf(firstNamed(m.node, 'value'))
    if (summary) out.push(summary)
    out.push(...docSections(m.node, 4))
  }
  return out
}

function renderType(type) {
  const out = [`# ${escapeText(type.name)}`, `**Namespace:** [\`${type.namespace}\`](./${nsFile(type.namespace)})  \n**Full name:** \`${type.fullName.replace(/`/g, '\\`')}\``]
  const summary = markdownOf(firstNamed(type.node, 'summary'))
  if (summary) out.push(summary)
  out.push(...docSections(type.node, 2).filter((s) => !s.startsWith('## Returns') && !s.startsWith('## Exceptions')))
  const remarks = markdownOf(firstNamed(type.node, 'remarks'))
  if (remarks) out.push('## Remarks', remarks)
  out.push(...memberSection('Constructors', type, type.ctors, true))
  out.push(...memberSection('Fields', type, type.fields, false))
  out.push(...memberSection('Properties', type, type.properties, false))
  out.push(...memberSection('Events', type, type.events, false))
  out.push(...memberSection('Methods', type, type.methods, true))
  return out.join('\n\n') + '\n'
}

function firstSentence(type) {
  return markdownOf(firstNamed(type.node, 'summary')).split('\n\n')[0].replace(/\|/g, '\\|')
}

function typeTable(list) {
  const rows = list.map((t) => `| [\`${t.name}\`](./${typeFile(t.fullName)}) | ${firstSentence(t)} |`)
  return ['| Type | Summary |', '| --- | --- |', ...rows].join('\n')
}

// ----------------------------------------------------------------- output

function groupByNamespace() {
  const groups = new Map()
  for (const t of [...types.values()].sort((a, b) => a.fullName.localeCompare(b.fullName))) {
    if (!groups.has(t.namespace)) groups.set(t.namespace, [])
    groups.get(t.namespace).push(t)
  }
  return new Map([...groups].sort(([a], [b]) => a.localeCompare(b)))
}

function write(file, content) {
  writeFileSync(join(OUT, file), content.endsWith('\n') ? content : content + '\n')
  return 1
}

function generate() {
  rmSync(OUT, { recursive: true, force: true })
  mkdirSync(OUT, { recursive: true })
  const groups = groupByNamespace()
  let files = 0

  const index = ['# API Reference', 'Generated from the XML documentation of the Terp packages.']
  for (const [ns, list] of groups) {
    index.push(`## [${ns}](./${nsFile(ns)})`, typeTable(list))
  }
  files += write('index.md', index.join('\n\n'))

  const sidebar = []
  for (const [ns, list] of groups) {
    files += write(`${nsFile(ns)}.md`, [`# ${ns} Namespace`, typeTable(list)].join('\n\n'))
    for (const t of list) files += write(`${typeFile(t.fullName)}.md`, renderType(t))
    sidebar.push({
      text: ns,
      collapsed: true,
      items: [
        { text: 'Overview', link: `/${OUTPUT_DIR}/${nsFile(ns)}` },
        ...list.map((t) => ({ text: t.name, link: `/${OUTPUT_DIR}/${typeFile(t.fullName)}` }))
      ]
    })
  }
  writeFileSync(join(OUT, '_sidebar.json'), JSON.stringify(sidebar, null, 2) + '\n')
  files += 1

  console.log(`API reference: ${types.size} types in ${groups.size} namespaces, ${files} files written to ${OUTPUT_DIR}/`)
}

async function main() {
  buildModel(await loadMembers())
  generate()
}

main().catch((err) => {
  console.error(err.message ?? err)
  process.exit(1)
})
