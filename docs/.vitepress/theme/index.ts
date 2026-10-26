import DefaultTheme from 'vitepress/theme-without-fonts'
import type { Theme } from 'vitepress'
import './fonts.css'
import './custom.css'
import InstallCommand from './components/InstallCommand.vue'
import PackageVersion from './components/PackageVersion.vue'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('InstallCommand', InstallCommand)
    app.component('PackageVersion', PackageVersion)
  }
} satisfies Theme
