import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Db4Net',
  description: 'Safe, SQL-shaped fluent query and command builder for Dapper.',
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }]
  ],
  themeConfig: {
    logo: '/favicon.svg',
    search: {
      provider: 'local',
      options: {
        locales: {
          zh: {
            translations: {
              button: {
                buttonText: '搜索',
                buttonAriaLabel: '搜索'
              },
              modal: {
                displayDetails: '显示详细列表',
                resetButtonTitle: '重置搜索',
                backButtonTitle: '关闭搜索',
                noResultsText: '没有找到相关结果',
                footer: {
                  selectText: '选择',
                  selectKeyAriaLabel: '回车',
                  navigateText: '切换',
                  navigateUpKeyAriaLabel: '向上箭头',
                  navigateDownKeyAriaLabel: '向下箭头',
                  closeText: '关闭',
                  closeKeyAriaLabel: 'Esc'
                }
              }
            }
          }
        }
      }
    },
    socialLinks: [
      { icon: 'github', link: 'https://github.com/IceCoffee1024/Db4Net' }
    ]
  },
  locales: {
    root: {
      label: 'English',
      lang: 'en-US',
      title: 'Db4Net',
      description: 'Safe, SQL-shaped fluent query and command builder for Dapper.',
      themeConfig: {
        nav: [
          { text: 'Guide', link: '/getting-started' },
          { text: 'GitHub', link: 'https://github.com/IceCoffee1024/Db4Net' },
          { text: 'NuGet', link: 'https://www.nuget.org/packages/Db4Net' }
        ],
        sidebar: [
          {
            text: 'Introduction',
            items: [
              { text: 'Getting Started', link: '/getting-started' },
              { text: 'Complete Example', link: '/complete-example' }
            ]
          },
          {
            text: 'Basic Usage',
            items: [
              { text: 'Select', link: '/select' },
              { text: 'Filters', link: '/filters' },
              { text: 'Ordering and Paging', link: '/ordering-and-paging' },
              { text: 'Insert', link: '/insert' },
              { text: 'Update', link: '/update' },
              { text: 'Delete', link: '/delete' }
            ]
          },
          {
            text: 'Advanced Usage',
            items: [
              { text: 'Entity Convenience', link: '/entity-convenience' },
              { text: 'Many Convenience', link: '/many-convenience' },
              { text: 'Conflict Inserts', link: '/conflict-inserts' },
              { text: 'Table Overrides', link: '/table-overrides' }
            ]
          },
          {
            text: 'Reference',
            items: [
              { text: 'Mapping', link: '/mapping' },
              { text: 'Dialects', link: '/dialects' },
              { text: 'Execution Options', link: '/execution-options' },
              { text: 'Testing', link: '/testing' },
              { text: 'Limitations', link: '/limitations' },
              { text: 'Changelog', link: '/changelog' }
            ]
          }
        ]
      }
    },
    zh: {
      label: '简体中文',
      lang: 'zh-CN',
      link: '/zh/',
      title: 'Db4Net',
      description: '面向 Dapper 的安全、贴近 SQL 顺序的 fluent query 和 command builder。',
      themeConfig: {
        nav: [
          { text: '指南', link: '/zh/getting-started' },
          { text: 'GitHub', link: 'https://github.com/IceCoffee1024/Db4Net' },
          { text: 'NuGet', link: 'https://www.nuget.org/packages/Db4Net' }
        ],
        sidebar: [
          {
            text: '介绍',
            items: [
              { text: '快速开始', link: '/zh/getting-started' },
              { text: '完整示例', link: '/zh/complete-example' }
            ]
          },
          {
            text: '基础用法',
            items: [
              { text: '查询', link: '/zh/select' },
              { text: '筛选', link: '/zh/filters' },
              { text: '排序与分页', link: '/zh/ordering-and-paging' },
              { text: '插入', link: '/zh/insert' },
              { text: '更新', link: '/zh/update' },
              { text: '删除', link: '/zh/delete' }
            ]
          },
          {
            text: '高级用法',
            items: [
              { text: '实体便捷方法', link: '/zh/entity-convenience' },
              { text: '批量便捷方法', link: '/zh/many-convenience' },
              { text: '冲突插入', link: '/zh/conflict-inserts' },
              { text: '表名覆盖', link: '/zh/table-overrides' }
            ]
          },
          {
            text: '参考',
            items: [
              { text: '映射', link: '/zh/mapping' },
              { text: '方言', link: '/zh/dialects' },
              { text: '执行选项', link: '/zh/execution-options' },
              { text: '测试', link: '/zh/testing' },
              { text: '限制', link: '/zh/limitations' },
              { text: '变更日志', link: '/zh/changelog' }
            ]
          }
        ]
      }
    }
  }
})
