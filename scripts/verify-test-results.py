"""Fail validation when tests fail, abort, or silently discover no tests."""
import argparse
from pathlib import Path
import xml.etree.ElementTree as ET

p = argparse.ArgumentParser()
p.add_argument('directory')
p.add_argument('--minimum-runs', type=int, default=1)
args = p.parse_args()
files = list(Path(args.directory).rglob('*.trx'))
assert len(files) >= args.minimum_runs, f'Expected {args.minimum_runs} runs, found {len(files)}'
totals = {'total': 0, 'passed': 0, 'failed': 0, 'skipped': 0}
for file in files:
    root = ET.parse(file).getroot()
    summary = root.find('.//{*}ResultSummary')
    assert summary is not None and summary.get('outcome') in {'Completed', 'Passed'}, f'Unsuccessful test run: {file}'
    counters = root.find('.//{*}Counters')
    assert counters is not None and int(counters.get('total', '0')) > 0, f'No tests: {file}'
    assert int(counters.get('failed', '0')) == 0, f'Failed tests: {file}'
    assert int(counters.get('aborted', '0')) == 0, f'Aborted tests: {file}'
    for key in totals:
        totals[key] += (int(counters.get('total', '0')) - int(counters.get('executed', '0'))
                        if key == 'skipped' else int(counters.get(key, '0')))
print(f'{len(files)} runs: {totals}')
