"""Regression for transient Windows sharing locks during atomic town export."""
from pathlib import Path
import importlib.util
import tempfile
import unittest
from unittest.mock import patch

source = Path(__file__).resolve().parents[1] / 'games/SinStarI/SourceAssets/Towns/Neris/NerisTownV1/Source/static_glb.py'
spec = importlib.util.spec_from_file_location('neris_static_glb', source)
writer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(writer)


class AtomicExportTests(unittest.TestCase):
    def test_transient_lock_retries_then_publishes(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder)/'town.glb'
            target.write_bytes(b'previous asset')
            original = Path.replace
            attempts = []

            def locked_twice(temporary, destination):
                attempts.append(destination)
                if len(attempts) < 3:
                    raise PermissionError('Simulated transient Windows lock')
                return original(temporary, destination)

            with patch.object(Path, 'replace', locked_twice), patch.object(writer.time, 'sleep'):
                writer.write(target, [])
            self.assertEqual(len(attempts), 3)
            self.assertEqual(target.read_bytes()[:4], b'glTF')
            self.assertFalse(target.with_suffix('.glb.tmp').exists())

    def test_persistent_lock_preserves_previous_asset_and_fails(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder)/'town.glb'
            target.write_bytes(b'previous asset')
            with patch.object(Path, 'replace', side_effect=PermissionError('Locked')) as replace:
                with patch.object(writer.time, 'sleep'), self.assertRaises(PermissionError):
                    writer.write(target, [])
            self.assertEqual(replace.call_count, 20)
            self.assertEqual(target.read_bytes(), b'previous asset')


if __name__ == '__main__':
    unittest.main()
