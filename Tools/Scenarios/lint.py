#!/usr/bin/env python3

import os
import re
import unicodedata

from luaparser import astnodes
from nova_script_parser import (get_node_name, normalize_dialogue,
                                parse_chapters, walk_functions)

in_dir = '../../Assets/Resources/Scenarios/'
template_filename = 'template.txt'


def is_special_char(c):
    return c != '\n' and unicodedata.category(c)[0] == 'C'


def check_code(code, line_num, anim_persist_tracked):
    for c in code:
        if is_special_char(c):
            print(f'Line {line_num}: special character U+{ord(c):04X} in code')

    if 'TODO' in code:
        print(f'Line {line_num}: TODO in code')

    check_anim_persist_override = False
    check_show = False
    check_trans = False
    try:
        for func_name, args, env in walk_functions(code):
            arg_names = [get_node_name(x) for x in args]

            for name in [func_name] + arg_names:
                if name == 'anim_persist_begin':
                    if anim_persist_tracked:
                        print(
                            f'Line {line_num}: anim_persist_begin() not match')
                    else:
                        anim_persist_tracked = True

                    if env:
                        check_anim_persist_override = True

                    if 'anim_persist' in env:
                        print(
                            f'Line {line_num}: anim_persist_begin() in anim_persist'
                        )

                elif name == 'anim_persist_end':
                    if anim_persist_tracked:
                        anim_persist_tracked = False
                    else:
                        print(f'Line {line_num}: anim_persist_end() not match')

                    if env:
                        check_anim_persist_override = True

                    if 'anim_persist' in env:
                        print(
                            f'Line {line_num}: anim_persist_end() in anim_persist'
                        )

            if func_name == 'anim':
                if env:
                    print(f'Line {line_num}: anim in anon function')

            elif func_name == 'anim_persist':
                if not anim_persist_tracked:
                    print(f'Line {line_num}: anim_persist not tracked')

                if check_anim_persist_override and not env:
                    print(
                        f'Line {line_num}: anim_persist overridden by anim_persist_begin() or anim_persist_end()'
                    )

                if 'anim_persist' in env:
                    print(f'Line {line_num}: anim_persist in anim_persist')

            elif func_name == 'show':
                if (not env and args and arg_names[0] != 'extra_text'):
                    check_show = True

            elif 'trans' in func_name:
                if (len(args) >= 2 and arg_names[0] == 'cam'
                        and not isinstance(args[1], astnodes.Nil)):
                    check_trans = True

    except Exception as e:
        print(f'Line {line_num}: error when parsing code: {e}')

    if check_show and check_trans:
        print(f'Line {line_num}: show() outside of trans()')

    return anim_persist_tracked


def check_dialogue(chara_name, dialogue, line_num):
    for c in dialogue:
        if is_special_char(c):
            print(
                f'Line {line_num}: special character U+{ord(c):04X} in dialogue'
            )

    match = re.compile('（TODO：(.*?)：.*?）').search(dialogue)
    if match:
        print(f'Line {line_num}: TODO: {match.group(1)} in dialogue')
    elif 'TODO' in dialogue:
        print(f'Line {line_num}: TODO in dialogue')

    dialogue = normalize_dialogue(dialogue)

    if chara_name:
        if not dialogue.startswith('“'):
            print(f'Line {line_num}: dialogue not start with quotation mark')
        if not dialogue.endswith('”'):
            print(f'Line {line_num}: dialogue not end with quotation mark')
    else:
        match = re.compile('.*?：“.*?”').fullmatch(dialogue)
        if match:
            print(f'Line {line_num}: quote with single colon')

    # if len(dialogue) > 54:
    #     print(f'Line {line_num}: dialogue longer than 54 chars')

    if any(x in dialogue for x in ',.?!;:\'"()'):
        print(f'Line {line_num}: half width punctuation in dialogue')


def lint_file(in_filename):
    with open(in_filename, 'r', encoding='utf-8') as f:
        chapters = parse_chapters(f, keep_line_num=True)

    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)
        anim_persist_tracked = False
        for code, chara_name, dialogue, line_num in entries:
            if code and not dialogue:
                print(f'Line {line_num}: code block with empty dialogue')

            if code:
                anim_persist_tracked = check_code(code, line_num,
                                                  anim_persist_tracked)

            if dialogue:
                check_dialogue(chara_name, dialogue, line_num)


def main():
    with open(template_filename, 'r', encoding='utf-8') as f_template:
        for line in f_template:
            if line.startswith('@include'):
                in_filename = os.path.join(in_dir, line.strip().split()[1])
                print(in_filename)
                lint_file(in_filename)
                print()


if __name__ == '__main__':
    main()
