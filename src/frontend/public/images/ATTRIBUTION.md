# 图片来源与处理说明

所有图片均来自 [Pexels](https://www.pexels.com/)，遵循 [Pexels License](https://www.pexels.com/license/)（可免费商用、无需署名，感谢摄影者）。

> 说明：当前终端缺少图片处理工具（无 ImageMagick / cwebp / Pillow），无法可靠下载并转换原图为 WebP。
> 下列文件需手动从来源页下载原图后，按「处理方式」转换并保存到对应路径。

## 待下载清单

| 文件名                 | 目标路径                                       | 原始来源页                                                                                     | 处理方式                                 |
| ---------------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------------------- | ---------------------------------------- |
| login-training.webp    | `public/images/login/login-training.webp`      | https://www.pexels.com/photo/business-training-course-18999470/                                | 裁剪/缩放至约 1280×1600，WebP，300–450KB |
| course-technology.webp | `public/images/courses/course-technology.webp` | https://www.pexels.com/photo/two-women-looking-at-the-code-at-laptop-1181263/                  | 裁剪 640×360，WebP，60–100KB             |
| course-management.webp | `public/images/courses/course-management.webp` | https://www.pexels.com/photo/colleagues-looking-at-sticky-notes-9301872/                       | 裁剪 640×360，WebP，60–100KB             |
| course-product.webp    | `public/images/courses/course-product.webp`    | https://www.pexels.com/photo/marketing-people-creative-desk-7688439/                           | 裁剪 640×360，WebP，60–100KB             |
| course-marketing.webp  | `public/images/courses/course-marketing.webp`  | https://www.pexels.com/photo/marketing-specialist-giving-presentation-at-the-meeting-17713776/ | 裁剪 640×360，WebP，60–100KB             |

## 统一处理说明

- 摄影者：见各来源页（待下载时补充作者署名）。
- 下载日期：2026-08-16。
- 许可证：Pexels License。
- 调色：课程封面统一做低饱和蓝灰处理，避免高饱和颜色干扰界面层级。
- 尺寸：课程封面统一 `640 × 360`（16:9），登录图按分栏比例约 `1280 × 1600`。
- 压缩：课程封面每张 60–100KB，登录图 300–450KB，禁止提交数 MB 原图。

## 占位降级

在图片文件尚未就位前，`CourseCover` 组件与登录页已实现渐变占位降级，页面不会因缺图报错或布局抖动。
