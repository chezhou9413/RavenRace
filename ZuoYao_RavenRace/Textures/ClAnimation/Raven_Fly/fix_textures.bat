@echo off
setlocal enabledelayedexpansion

echo [ChezhouLib] 正在为序列帧生成 north 和 east 副本...

:: 遍历所有以 _south.png 结尾的文件
for %%f in (*_south.png) do (
    set "filename=%%~nf"
    
    :: 去掉文件名末尾的 _south (6个字符) 获取前缀
    set "basename=!filename:~0,-6!"
    
    echo 正在处理: !basename!
    
    :: 复制生成 _north.png
    if not exist "!basename!_north.png" (
        copy "%%f" "!basename!_north.png" >nul
    )
    
    :: 复制生成 _east.png
    if not exist "!basename!_east.png" (
        copy "%%f" "!basename!_east.png" >nul
    )
)

echo.
echo [完成] 现在你可以安全使用 Graphic_Multi 加载了！
pause