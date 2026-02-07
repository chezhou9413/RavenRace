@echo off
chcp 65001 > nul
title PNG文件名收集器

:: 设置输出文件名
set "output_file=png_filenames.txt"

:: 如果已存在旧的输出文件，先删除它，确保每次都是全新的
if exist "%output_file%" del "%output_file%"

echo 正在递归收集当前目录及所有子目录下的PNG图片文件名...
echo.

:: 使用 for /R 递归遍历当前文件夹(.)及其所有子文件夹
:: %%F 会包含文件的完整路径
:: 将每个PNG文件的完整路径和文件名写入输出文件
(for /R . %%F in (*.png) do (
    echo "%%F"
)) > "%output_file%"

:: 统计找到的文件数量
set count=0
for /f "usebackq delims=" %%i in (`find /c ".png" "%output_file%" 2^>nul`) do set count=%%i

echo 完成！共找到 %count% 个PNG文件
echo 所有PNG文件名已保存到 %output_file%
echo.
echo 文件列表预览（前10个）：
echo ========================================
if exist "%output_file%" (
    setlocal enabledelayedexpansion
    set line=0
    for /f "usebackq delims=" %%a in ("%output_file%") do (
        set /a line+=1
        if !line! leq 10 echo !line!. %%a
    )
    endlocal
)
echo ========================================
echo.
pause