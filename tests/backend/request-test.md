# 1. 打开你的 training-requests.http 文件
在 VS Code 中打开 tests/backend/training-requests.http 文件。
(此时需要保持程序已在运行)

# 2. 先获取 Token
运行tests/api/auth.http，获取真正的 Token。

点击 auth.http 中的 ### Login as Oracle seed admin user 上方出现的 Send Request 按钮。

从响应中复制 token 的值。

# 3. 配置变量
打开 training-requests.http 文件。

将 @employeeToken、@managerToken、@hrToken 的值替换为你刚刚获取的 Token。

将 @baseUrl 的值改为后端的实际地址（例如 https://localhost:7156）。

先放一个真实存在的 requestId。

# 4. 运行测试
在每个用例上方，都会出现一个灰色的 Send Request 按钮。

按顺序点击这些按钮，从“提交申请”开始，一直到“主管审批”、“HR备案”。

观察右侧打开的响应窗口。