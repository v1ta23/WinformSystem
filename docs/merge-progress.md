# 项目合并进度

## 当前阶段

按 `docs/architecture-plan.md` 的思路，当前先完成了第一批可运行迁移：

1. 保留旧项目 `D:\jetbrains\c#\WinFormsApp1` 不动，作为业务逻辑来源。
2. 在 `D:\jetbrains\c#\WinFormsApp2` 内把原单工程拆成：
   - `App.Core`
   - `App.Infrastructure`
   - `App.WinForms`
3. 先迁移旧项目里最明确的一组能力：
   - 登录校验
   - 注册校验
   - 记住密码
   - SQL Server 用户表访问
4. 将新项目仪表盘改成 `Controller -> Service -> Repository -> ViewModel -> Form` 数据流。

## 本次已接入的功能

- 登录页：替换为新项目内的 `App.WinForms/Views/LoginForm.cs`
- 注册页：替换为新项目内的 `App.WinForms/Views/RegisterForm.cs`
- 登录业务：从旧项目 `Form1.cs` 抽到 `App.Core/Services/AuthenticationService.cs`
- 注册业务：从旧项目 `Register.cs` 抽到 `App.Core/Services/AuthenticationService.cs`
- 数据访问：从旧项目窗体事件抽到 `App.Infrastructure/Repositories/SqlUserRepository.cs`
- 本地记住密码：抽到 `App.Infrastructure/Repositories/FileRememberMeRepository.cs`
- 仪表盘展示：由 `App.WinForms/Controllers/DashboardController.cs` 提供数据

## 下一批建议迁移

1. 把旧项目 `Index` 页上的真实业务入口梳理出来，逐项补到新仪表盘导航。
2. 将数据库连接串从 `AppCompositionRoot` 提取到配置文件。
3. 为认证服务补最基本的单元测试。
4. 当新界面覆盖旧流程后，再决定是否下线旧窗体工程。
