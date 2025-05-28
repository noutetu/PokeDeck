
# check_numpy_path.py
import sys
import numpy

print("sys.path の中身：")
for path in sys.path:
    print(" -", path)

print("\nnumpy のパス：")
print(numpy.__file__)
