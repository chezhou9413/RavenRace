@echo off
chcp 65001 > nul

:: 设置输出文件名
set "output_file=combined_output_all.txt"

:: 如果已存在旧的输出文件，先删除它，确保每次都是全新的
if exist "%output_file%" del "%output_file%"

echo 正在递归汇总当前目录及所有子目录下的 .cs 和 .xml 文件，请稍候...

:: 使用 for /R 递归遍历当前文件夹(.)及其所有子文件夹
:: %%F 会包含文件的完整路径
:: 将每个文件的文件名和内容追加到输出文件中
(for /R . %%F in (*.cs *.xml) do (
    echo =
    echo File: %%F
    echo =
    echo.
    type "%%F"
    echo.
    echo.
)) > "%output_file%"

echo.
echo 完成！所有 .cs 和 .xml 文件已汇总到 %output_file%
pause