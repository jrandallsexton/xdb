"""
Writes Eastern.tzif: US Eastern time as Windows' "Eastern Standard Time" defines it, for hosts that read IANA
zone files (Linux). The legacy service ran on Windows, whose zone has two rules:
  - up to 2006: daylight time from the first Sunday of April to the last Sunday of October (02:00 local),
    applied to every earlier year as well (Windows has no older history);
  - from 2007: from the second Sunday of March to the first Sunday of November.
IANA data instead carries the real history (local mean time -04:56:02 before 1883, wartime rules, ...), so dates
the legacy wire writes (year 1 included) would differ. Transitions are written for years 1-2037 and the POSIX
footer covers later years.

Usage: python generate-eastern-tzif.py <output path>
"""
import calendar
import struct
import sys
from datetime import datetime, timedelta

EST, EDT = -5 * 3600, -4 * 3600
EPOCH = datetime(1970, 1, 1)


def nth_sunday(year, month, n):
    first = datetime(year, month, 1)
    first_sunday = first + timedelta(days=(6 - first.weekday()) % 7)
    return first_sunday + timedelta(weeks=n - 1)


def last_sunday(year, month):
    last = datetime(year, month, calendar.monthrange(year, month)[1])
    return last - timedelta(days=(last.weekday() + 1) % 7)


def seconds(local, offset):
    """UTC seconds since the epoch of a local wall time with the given offset."""
    delta = (local - EPOCH) - timedelta(seconds=offset)
    return delta.days * 86400 + delta.seconds


transitions = []  # (utc seconds, type index)
for year in range(1, 2038):
    if year <= 2006:
        start, end = nth_sunday(year, 4, 1), last_sunday(year, 10)
    else:
        start, end = nth_sunday(year, 3, 2), nth_sunday(year, 11, 1)
    transitions.append((seconds(start.replace(hour=2), EST), 1))
    transitions.append((seconds(end.replace(hour=2), EDT), 0))

types = [(EST, 0, 0), (EDT, 1, 4)]  # (utoff, isdst, abbreviation index)
abbreviations = b"EST\0EDT\0"


def header(version, timecnt):
    # magic, version, 15 reserved bytes, then isutcnt, isstdcnt, leapcnt, timecnt, typecnt, charcnt
    return b"TZif" + version + b"\0" * 15 + struct.pack(">6l", 0, 0, 0, timecnt, len(types), len(abbreviations))


def body(time_format, items):
    data = b"".join(struct.pack(time_format, t) for t, _ in items)
    data += bytes(index for _, index in items)
    data += b"".join(struct.pack(">lbB", utoff, isdst, abbr) for utoff, isdst, abbr in types)
    return data + abbreviations


with open(sys.argv[1], "wb") as f:
    # Version 1 block (32-bit times): no transitions; readers of version 2 skip it.
    f.write(header(b"2", 0) + body(">l", []))
    f.write(header(b"2", len(transitions)) + body(">q", transitions))
    f.write(b"\nEST5EDT,M3.2.0,M11.1.0\n")
