import { useMemo, useRef } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { buildSwooshGeometry } from './SwooshGeometry';
import { useTabVisible } from './useTabVisible';

interface SwooshMeshProps {
  reducedMotion: boolean;
}

function SwooshMesh({ reducedMotion }: SwooshMeshProps) {
  const geometry = useMemo(() => buildSwooshGeometry(), []);
  const groupRef = useRef<THREE.Group>(null);
  const lightRef = useRef<THREE.PointLight>(null);

  useFrame((state, delta) => {
    if (reducedMotion) return;
    const group = groupRef.current;
    if (!group) return;

    const t = state.clock.getElapsedTime();

    // Slow ambient drift — a full cycle takes several seconds, never a spin.
    group.position.y = Math.sin(t * 0.35) * 0.12;
    group.rotation.z = -0.55 + Math.cos(t * 0.2) * 0.04;

    // Subtle cursor influence, lerped in — capped well under the 5-8 degree
    // ceiling and never a hard snap to the pointer.
    const maxYaw = THREE.MathUtils.degToRad(7);
    const maxPitch = THREE.MathUtils.degToRad(4);
    const targetY = -0.3 + state.pointer.x * maxYaw;
    const targetX = Math.sin(t * 0.25) * 0.05 - state.pointer.y * maxPitch;
    const smoothing = Math.min(1, delta * 1.5);
    group.rotation.y += (targetY - group.rotation.y) * smoothing;
    group.rotation.x += (targetX - group.rotation.x) * smoothing;

    if (lightRef.current) {
      lightRef.current.position.x = Math.sin(t * 0.3) * 3.5;
      lightRef.current.position.z = 2 + Math.cos(t * 0.3) * 2;
    }
  });

  return (
    <group ref={groupRef} rotation={[0, -0.3, -0.55]}>
      <mesh geometry={geometry}>
        <meshPhysicalMaterial
          color="#f2b705"
          emissive="#3a2600"
          emissiveIntensity={0.5}
          roughness={0.3}
          metalness={0.25}
          clearcoat={0.7}
          clearcoatRoughness={0.25}
          reflectivity={0.6}
        />
      </mesh>
      <pointLight ref={lightRef} position={[3.5, 2, 2]} intensity={55} color="#fff6d8" />
    </group>
  );
}

interface SwooshSceneProps {
  reducedMotion: boolean;
}

export function SwooshScene({ reducedMotion }: SwooshSceneProps) {
  const isTabVisible = useTabVisible();

  return (
    <Canvas
      dpr={[1, 1.75]}
      frameloop={isTabVisible ? 'always' : 'never'}
      camera={{ position: [0, 0.2, 6.5], fov: 38 }}
      gl={{ antialias: true, alpha: true }}
      style={{ background: 'transparent' }}
    >
      <ambientLight intensity={0.35} />
      <directionalLight position={[-4, 3, 2]} intensity={1.1} color="#ffffff" />
      <directionalLight position={[2, -2, -3]} intensity={0.3} color="#ffb672" />
      <SwooshMesh reducedMotion={reducedMotion} />
    </Canvas>
  );
}
