@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul
title RimWorld Mod图片资源路径收集器

:: ================= 配置部分 =================
set "output_file=png_filenames.txt"
:: ===========================================

:: 获取当前运行目录作为根目录，并确保末尾有反斜杠
set "root_dir=%~dp0"
echo 当前扫描根目录: %root_dir%
echo.

:: 准备临时文件
set "temp_file=%temp%\rim_png_scan.tmp"
if exist "%output_file%" del "%output_file%"
if exist "%temp_file%" del "%temp_file%"

echo 正在扫描目录结构和PNG文件...

:: 1. 扫描所有文件夹 (包括空文件夹)
:: dir /s /b /ad 表示递归(s)、精简模式(b)、只看目录(ad)
for /f "delims=" %%D in ('dir /s /b /ad') do (
    set "full_path=%%D"
    
    :: 核心逻辑：将全路径中的根目录部分替换为空，得到相对路径
    set "rel_path=!full_path:%root_dir%=!"
    
    :: 写入临时文件，文件夹末尾加个 \ 以便区分
    echo !rel_path!\>> "%temp_file%"
)

:: 2. 扫描所有PNG文件
:: dir /s /b /a-d *.png 表示递归、精简、非目录(a-d)、只要png
for /f "delims=" %%F in ('dir /s /b /a-d *.png') do (
    set "full_path=%%F"
    set "rel_path=!full_path:%root_dir%=!"
    echo !rel_path!>> "%temp_file%"
)

echo 正在整理和排序输出结果...

:: 3. 对结果进行排序并写入最终文件
:: 排序可以让文件显示在对应的文件夹路径下方，视觉上像目录树
sort "%temp_file%" > "%output_file%"

:: 清理临时文件
del "%temp_file%"

:: 统计结果
set count_png=0
set count_dir=0
for /f "usebackq delims=" %%i in (`find /c ".png" "%output_file%" 2^>nul`) do set count_png=%%i
for /f "usebackq delims=" %%i in (`find /c "\" "%output_file%" 2^>nul`) do set count_dir=%%i
:: 文件夹数量统计可能会把包含路径的文件也算进去，这里仅做粗略参考，或者直接用行数减去png数

echo.
echo ========================================
echo 处理完成！
echo 结果已保存至: %output_file%
echo.
echo [预览前 15 行内容]
echo ----------------------------------------
if exist "%output_file%" (
    set line=0
    for /f "usebackq delims=" %%a in ("%output_file%") do (
        set /a line+=1
        if !line! leq 15 echo %%a
    )
)
echo ----------------------------------------
echo.
echo  * 注：以 "\" 结尾的是文件夹，其余为PNG图片。
echo ========================================
pause