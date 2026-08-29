import * as THREE from 'three';

/**
 * A single continuous sculptural stroke — an asymmetrical hook that sweeps in
 * from the upper right, arcs down and left, and curls back toward where the
 * "SA Harvest" wordmark sits. Deliberately hand-placed, not a generic blob/
 * torus/wave: the curl at the tail is what makes the swoosh visually point
 * into the typography instead of just floating near it.
 */
const CONTROL_POINTS: THREE.Vector3[] = [
  new THREE.Vector3(3.4, 1.75, -0.5),
  new THREE.Vector3(2.15, 2.05, 0.35),
  new THREE.Vector3(0.75, 1.4, 0.7),
  new THREE.Vector3(-0.5, 0.45, 0.15),
  new THREE.Vector3(-1.05, -0.55, -0.35),
  new THREE.Vector3(-0.75, -1.35, -0.15),
  new THREE.Vector3(0.15, -1.6, 0.25),
];

// Thin at the very tip, widest through the body, tapering back to a point as
// it flows into the wordmark — that taper is what sells "flowing into", not
// a tube of constant thickness ending abruptly.
const TIP_TAPER_END = 0.12;
const TAIL_TAPER_START = 0.7;
const MAJOR_RADIUS = 0.26; // ribbon width
const MINOR_RADIUS = 0.075; // ribbon thickness
const TOTAL_TWIST = Math.PI * 0.9; // slight twist along the length, not a full spiral

function smoothstep(edge0: number, edge1: number, x: number): number {
  const t = Math.min(Math.max((x - edge0) / (edge1 - edge0), 0), 1);
  return t * t * (3 - 2 * t);
}

interface BuildSwooshGeometryOptions {
  tubularSegments?: number;
  radialSegments?: number;
}

export function buildSwooshGeometry({
  tubularSegments = 140,
  radialSegments = 20,
}: BuildSwooshGeometryOptions = {}): THREE.BufferGeometry {
  const curve = new THREE.CatmullRomCurve3(CONTROL_POINTS, false, 'catmullrom', 0.5);
  const curvePoints = curve.getSpacedPoints(tubularSegments);
  // Rotation-minimizing frames (three's implementation) rather than naive
  // Frenet-Serret — avoids twisting artifacts where curvature nears zero.
  const frames = curve.computeFrenetFrames(tubularSegments, false);

  const positions: number[] = [];
  const indices: number[] = [];

  for (let i = 0; i <= tubularSegments; i++) {
    const t = i / tubularSegments;
    const center = curvePoints[i];
    const normal = frames.normals[i];
    const binormal = frames.binormals[i];

    const taper = smoothstep(0, TIP_TAPER_END, t) * (1 - smoothstep(TAIL_TAPER_START, 1, t));
    const twist = t * TOTAL_TWIST;
    const cosTwist = Math.cos(twist);
    const sinTwist = Math.sin(twist);

    for (let j = 0; j <= radialSegments; j++) {
      const theta = (j / radialSegments) * Math.PI * 2;
      const cx = Math.cos(theta) * MAJOR_RADIUS * taper;
      const cy = Math.sin(theta) * MINOR_RADIUS * taper;

      // Rotate the elliptical cross-section within its own (normal, binormal)
      // plane by `twist` — this is what makes it a twisted ribbon rather than
      // a tube that merely follows the curve.
      const rx = cx * cosTwist - cy * sinTwist;
      const ry = cx * sinTwist + cy * cosTwist;

      const vertex = new THREE.Vector3()
        .copy(center)
        .addScaledVector(normal, rx)
        .addScaledVector(binormal, ry);

      positions.push(vertex.x, vertex.y, vertex.z);
    }
  }

  const ring = radialSegments + 1;
  for (let i = 0; i < tubularSegments; i++) {
    for (let j = 0; j < radialSegments; j++) {
      const a = i * ring + j;
      const b = a + ring;
      const c = a + 1;
      const d = b + 1;
      indices.push(a, b, c, b, d, c);
    }
  }

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals();
  return geometry;
}
