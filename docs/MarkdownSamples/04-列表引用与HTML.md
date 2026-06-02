# 04 列表、引用与 HTML fallback

这份文件用于检查列表嵌套、任务列表、引用块内的复杂内容，以及 HTML 内容的 fallback 渲染。当前控件不执行 HTML，只把 HTML 文本安全显示出来。

## 观察点

- 有序列表编号宽度应随数字位数自动调整。
- 任务列表的勾选框应只读显示，不应影响文本选择。
- 引用块内的段落、列表、代码和表格应保持统一缩进。
- HTML inline 和 HTML block 应以文本形式显示，不应被浏览器式执行。

## 多级列表

1. 计划阶段
   1. 明确渲染能力
   2. 准备覆盖样例
   3. 记录观察点
2. 实现阶段
   - 更新固定资源 Key
   - 删除控件级排版主题属性
   - 保留 Markdown AST 渲染逻辑
3. 验证阶段
   - [x] 浅色主题
   - [x] 深色主题
   - [ ] 自定义业务主题

10. 双位数编号测试
11. 编号列应比个位数更宽
12. 文本仍应左对齐

## 引用内复杂内容

> 引用第一段：用于检查边线和背景。
>
> - 引用内列表第一项
> - 引用内列表第二项，包含 `inline code`
>
> ```text
> quoted code block
> line 2
> ```
>
> | 项 | 值 |
> | --- | --- |
> | nested table | ok |

## HTML inline

这一行包含 `<kbd>Ctrl</kbd>`、`<span class="token">token</span>` 和 `<br>`，它们应作为文本安全显示。

## HTML block

<section class="notice">
  <strong>这是一段 HTML block。</strong>
  <p>MarkdownViewer 应显示原始 HTML 文本，而不是执行或套用 HTML 样式。</p>
</section>

## 未知或扩展语法

::: warning
这是常见文档工具的容器语法。当前控件没有专门解析它时，应作为普通文本或 fallback 内容呈现。
:::
