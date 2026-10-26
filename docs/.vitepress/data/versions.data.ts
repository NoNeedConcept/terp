import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineLoader } from 'vitepress'

/** Package name -> version, read from release-please's manifest at build time. */
export type Versions = Record<string, string>

declare const data: Versions
export { data }

const manifest = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  '../../../.release-please-manifest.json'
)

export default defineLoader({
  watch: [manifest],
  load(): Versions {
    const raw = JSON.parse(fs.readFileSync(manifest, 'utf8')) as Record<string, string>
    const out: Versions = {}
    for (const [key, version] of Object.entries(raw)) {
      out[path.posix.basename(key.replace(/\\/g, '/'))] = version
    }
    return out
  }
})
