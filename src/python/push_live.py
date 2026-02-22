import shutil
import os
import glob
import sys
import argparse

parser = argparse.ArgumentParser()
parser.add_argument("--reload", type=str, default=None) # For example '{VK_SHIFT down}{VK_MENU down}{DEL}{VK_MENU up}{VK_SHIFT up}'

args = parser.parse_args()

home_dir = os.path.expanduser("~")

import utils

utils.check_dependencies("Zenithar", "## DependsOn:", True)
utils.check_dependencies("Zenithar", "## OptionalDependsOn:", False)

utils.push(f"{home_dir}/Documents/Elder Scrolls Online/live/AddOns/Zenithar", [])
utils.push(f"{home_dir}/Documents/Elder Scrolls Online/pts/AddOns/Zenithar", [])

if args.reload:
    utils.reload_eso(args.reload)
