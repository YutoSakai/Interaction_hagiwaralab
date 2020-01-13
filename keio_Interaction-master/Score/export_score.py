import json
import sys
import time
from collections import OrderedDict
CTRL_C = 3

try:
    from msvcrt import getch
except ImportError:
    def getch():
        import sys
        import tty
        import termios
        fd = sys.stdin.fileno()
        old = termios.tcgetattr(fd)
        try:
            tty.setraw(fd)
            return sys.stdin.read(1)
        finally:
            termios.tcsetattr(fd, termios.TCSADRAIN, old)

d1 = OrderedDict()
d1["title"] = "Shining Star"
d1["bpm"] = "158"

start_time = time.time()

timing = []
while True:
    d2 = OrderedDict()
    key = ord(getch())
    if key == CTRL_C:
        break

    elif chr(key) == "d":
        message = 'valid input, {0}'.format(chr(key))
        print(message)
        d2["timing"] = str((time.time() - start_time) * 1000)
        d2["type"] = "don"
        timing.append(d2)

    elif chr(key) == "k":
        message = 'valid input, {0}'.format(chr(key))
        print(message)
        d2["timing"] = str((time.time() - start_time) * 1000)
        d2["type"] = "ka"
        timing.append(d2)

    else:
        message = 'invalid input, {0}'.format(chr(key))
        print(message)


d1["notes"] = timing

with open('test1.json', 'w') as f:
    json.dump(d1, f, indent=4)

