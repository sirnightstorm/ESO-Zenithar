import subprocess
import re
import shutil
import os
import glob
import sys
import argparse

parser = argparse.ArgumentParser()
parser.add_argument("--version", type=str) # For example '{VK_SHIFT down}{VK_MENU down}{DEL}{VK_MENU up}{VK_SHIFT up}'
args = parser.parse_args()

def copy(wildcard, dest):
    for file in glob.glob(wildcard):
        print(file)
        shutil.copy(file, dest)


version = args.version
versionMatch = re.search(r"^(\d+)\.(\d+)\.(\d+)", version)
addOnVersion = int(versionMatch.group(1))*10000 + int(versionMatch.group(2))*100 + int(versionMatch.group(3))

print(f"AddOnVersion {addOnVersion}")

if os.path.exists("build/Zenithar"):
    shutil.rmtree('build/Zenithar')

os.makedirs('build/Zenithar/media', exist_ok=True)

copy(r'src/lua/*.lua', 'build/Zenithar')
copy(r'src/xml/*.xml', 'build/Zenithar')
copy(r'build/media/*.dds', 'build/Zenithar/media')

# shutil.copytree('media', 'build/Zenithar/media')
# shutil.copytree('lang', 'build/Zenithar/lang')

with open('src/Zenithar.addon', 'r') as inFile:
    txt = inFile.read()
    txt = re.sub(r'## Version: \w+', f"## Version: {version}", txt)
    txt = re.sub(r'## AddOnVersion: \w+', f"## AddOnVersion: {addOnVersion}", txt)
    with open('build/Zenithar/Zenithar.addon', 'w') as outFile:
        outFile.write(txt)

#with open('Zenithar.lua', 'r') as inFile:
#    lua = inFile.read()
#    lua = re.sub(r'appVersion = "[^"]*"', f'appVersion = "{version}"', lua)
#    with open('build/Zenithar/Zenithar.lua', 'w') as outFile:
#        outFile.write(lua)

#shutil.make_archive(f"build/Zenithar-v{version}", 'zip', root_dir='build', base_dir='Zenithar')
