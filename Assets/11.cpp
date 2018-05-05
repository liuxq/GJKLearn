// ### have a return code to tell if we really moved or not

// Using swept code & direct position update (no physics engine)
// This function is the generic character controller logic, valid for all swept volumes
PxControllerCollisionFlags SweepTest::moveCharacter(
					const InternalCBData_FindTouchedGeom* userData,
					const InternalCBData_OnHit* userHitData,
					SweptVolume& volume,
					const PxVec3& direction,
					const UserObstacles& userObstacles,
					float min_dist,
					const PxControllerFilters& filters,
					bool constrainedClimbingMode,
					bool standingOnMoving
					 )
{
	bool standingOnMovingUp = standingOnMoving;

	mFlags &= ~STF_HIT_NON_WALKABLE;
	PxControllerCollisionFlags CollisionFlags = PxControllerCollisionFlags(0);
	const PxU32 maxIter = MAX_ITER;	// 1 for "collide and stop"
	const PxU32 maxIterSides = maxIter;
	const PxU32 maxIterDown = ((mFlags & STF_WALK_EXPERIMENT) && mUserParams.mNonWalkableMode==PxControllerNonWalkableMode::ePREVENT_CLIMBING_AND_FORCE_SLIDING) ? maxIter : 1;
//	const PxU32 maxIterDown = 1;

	// ### this causes the artificial gap on top of chars
	float stepOffset = mUserParams.mStepOffset;	// Default step offset can be cancelled in some cases.

	// Save initial height
	const PxVec3& upDirection = mUserParams.mUpDirection;
//	const PxExtended originalHeight = volume.mCenter[upDirection];
	const PxVec3 volumeCenter = toVec3(volume.mCenter);
	const PxExtended originalHeight = volumeCenter.dot(upDirection);
	const PxExtended originalBottomPoint = originalHeight - volume.mHalfHeight;	// UBI

	// TEST! Disable auto-step when flying. Not sure this is really useful.
//	if(direction[upDirection]>0.0f)
	const float dir_dot_up = direction.dot(upDirection);
//printf("%f\n", dir_dot_up);
	if(dir_dot_up>0.0f)
	{
		mFlags |= STF_IS_MOVING_UP;

		// PT: this makes it fail on a platform moving up when jumping
		// However if we don't do that a jump when moving up a slope doesn't work anymore!
		// Not doing this also creates jittering when a capsule CCT jumps against another capsule CCT
		if(!standingOnMovingUp)	// PT: if we're standing on something moving up it's safer to do the up motion anyway, even though this won't work well before we add the flag in TA13542
		{
//			static int count=0;	printf("Cancelling step offset... %d\n", count++);
			stepOffset = 0.0f;
		}
	}
	else
	{
		mFlags &= ~STF_IS_MOVING_UP;
	}

	// Decompose motion into 3 independent motions: up, side, down
	// - if the motion is purely down (gravity only), the up part is needed to fight accuracy issues. For example if the
	// character is already touching the geometry a bit, the down sweep test might have troubles. If we first move it above
	// the geometry, the problems disappear.
	// - if the motion is lateral (character moving forward under normal gravity) the decomposition provides the autostep feature
	// - if the motion is purely up, the down part can be skipped

	PxVec3 UpVector(0.0f, 0.0f, 0.0f);
	PxVec3 DownVector(0.0f, 0.0f, 0.0f);

	PxVec3 normal_compo, tangent_compo;
	Ps::decomposeVector(normal_compo, tangent_compo, direction, upDirection);

//	if(direction[upDirection]<0.0f)
	if(dir_dot_up<=0.0f)
//		DownVector[upDirection] = direction[upDirection];
		DownVector = normal_compo;
	else
//		UpVector[upDirection] = direction[upDirection];
		UpVector = normal_compo;

//	PxVec3 SideVector = direction;
//	SideVector[upDirection] = 0.0f;
	PxVec3 SideVector = tangent_compo;

	// If the side motion is zero, i.e. if the character is not really moving, disable auto-step.
	// This is important to prevent the CCT from automatically climbing on small objects that move
	// against it. We should climb over those only if there's a valid side motion from the player.
	const bool sideVectorIsZero = !standingOnMovingUp && Ps::isAlmostZero(SideVector);	// We can't use PxVec3::isZero() safely with arbitrary up vectors
	// #### however if we do this the up pass is disabled, with bad consequences when the CCT is on a dynamic object!!
	// ### this line makes it possible to push other CCTs by jumping on them
//	const bool sideVectorIsZero = false;
//	printf("sideVectorIsZero: %d\n", sideVectorIsZero);

//	if(!SideVector.isZero())
	if(!sideVectorIsZero)
//		UpVector[upDirection] += stepOffset;
		UpVector += upDirection*stepOffset;
//	printf("stepOffset: %f\n", stepOffset);

	// ==========[ Initial volume query ]===========================
	// PT: the main difference between this initial query and subsequent ones is that we use the
	// full direction vector here, not the components along each axis. So there is a good chance
	// that this initial query will contain all the motion we need, and thus subsequent queries
	// will be skipped.
	{
		PxExtendedBounds3 temporalBox;
		volume.computeTemporalBox(*this, temporalBox, volume.mCenter, direction);

		// Gather touched geoms
		updateTouchedGeoms(userData, userObstacles, temporalBox, filters, SideVector);
	}

	// ==========[ UP PASS ]===========================

	mCachedTriIndexIndex = 0;
	const bool performUpPass = true;
	PxU32 NbCollisions=0;

	PxU32 maxIterUp;
	if(mUserParams.mPreventVerticalSlidingAgainstCeiling)
		maxIterUp = 1;
	else
		maxIterUp = Ps::isAlmostZero(SideVector) ? maxIter : 1;

	if(performUpPass)
	{
//		printf("%f | %f | %f\n", UpVector.x, UpVector.y, UpVector.z);

		// Prevent user callback for up motion. This up displacement is artificial, and only needed for auto-stepping.
		// If we call the user for this, we might eventually apply upward forces to objects resting on top of us, even
		// if we visually don't move. This produces weird-looking motions.
//		mValidateCallback = false;
		// PT: actually I think the previous comment is wrong. It's not only needed for auto-stepping: when the character
		// jumps there's a legit up motion and the lack of callback in that case could need some object can't be pushed
		// by the character's 'head' (for example). So I now think it's better to use the callback all the time, and
		// let users figure out what to do using the available state (like "isMovingUp", etc).
//		mValidateCallback = true;

		// In the walk-experiment we explicitly want to ban any up motions, to avoid characters climbing slopes they shouldn't climb.
		// So let's bypass the whole up pass.
		if(!(mFlags & STF_WALK_EXPERIMENT))
		{
			// ### maxIter here seems to "solve" the V bug
			if(doSweepTest(userData, userHitData, userObstacles, volume, UpVector, SideVector, maxIterUp, &NbCollisions, min_dist, filters, SWEEP_PASS_UP))
			{
				if(NbCollisions)
				{
					CollisionFlags |= PxControllerCollisionFlag::eCOLLISION_UP;

					// Clamp step offset to make sure we don't undo more than what we did
//					PxExtended Delta = volume.mCenter[upDirection] - originalHeight;
					PxExtended Delta = toVec3(volume.mCenter).dot(upDirection) - originalHeight;
					if(Delta<stepOffset)
					{
						stepOffset=float(Delta);
					}
				}
			}
		}
	}

	// ==========[ SIDE PASS ]===========================

	mCachedTriIndexIndex = 1;
//	mValidateCallback = true;
	const bool PerformSidePass = true;

	mFlags &= ~STF_VALIDATE_TRIANGLE_SIDE;
	if(PerformSidePass)
	{
		NbCollisions=0;
		//printf("BS:%.2f %.2f %.2f NS=%d\n", volume.mCenter.x, volume.mCenter.y, volume.mCenter.z, mNbCachedStatic);
		if(doSweepTest(userData, userHitData, userObstacles, volume, SideVector, SideVector, maxIterSides, &NbCollisions, min_dist, filters, SWEEP_PASS_SIDE))
		{
			if(NbCollisions)
				CollisionFlags |= PxControllerCollisionFlag::eCOLLISION_SIDES;
		}
		//printf("AS:%.2f %.2f %.2f NS=%d\n", volume.mCenter.x, volume.mCenter.y, volume.mCenter.z, mNbCachedStatic);

		if(1 && constrainedClimbingMode && volume.getType()==SweptVolumeType::eCAPSULE && !(mFlags & STF_VALIDATE_TRIANGLE_SIDE))
		{
			const float capsuleRadius = static_cast<const SweptCapsule&>(volume).mRadius;

			const float sideM = SideVector.magnitude();
			if(sideM<capsuleRadius)
			{
				const PxVec3 sensor = SideVector.getNormalized() * capsuleRadius;
				
				mFlags &= ~STF_VALIDATE_TRIANGLE_SIDE;
				NbCollisions=0;
				//printf("BS:%.2f %.2f %.2f NS=%d\n", volume.mCenter.x, volume.mCenter.y, volume.mCenter.z, mNbCachedStatic);
				const PxExtendedVec3 saved = volume.mCenter;
				doSweepTest(userData, userHitData, userObstacles, volume, sensor, SideVector, 1, &NbCollisions, min_dist, filters, SWEEP_PASS_SENSOR);
				volume.mCenter = saved;
			}
		}
	}

	// ==========[ DOWN PASS ]===========================

	mCachedTriIndexIndex = 2;
	const bool PerformDownPass = true;

	if(PerformDownPass)
	{
		NbCollisions=0;

//		if(!SideVector.isZero())	// We disabled that before so we don't have to undo it in that case
		if(!sideVectorIsZero)		// We disabled that before so we don't have to undo it in that case
//			DownVector[upDirection] -= stepOffset;	// Undo our artificial up motion
			DownVector -= upDirection*stepOffset;	// Undo our artificial up motion

		mFlags &= ~STF_VALIDATE_TRIANGLE_DOWN;
		mTouchedShape = NULL;
		mTouchedActor = NULL;
		mTouchedObstacleHandle	= INVALID_OBSTACLE_HANDLE;

		// min_dist actually makes a big difference :(
		// AAARRRGGH: if we get culled because of min_dist here, mValidateTriangle never becomes valid!
		if(doSweepTest(userData, userHitData, userObstacles, volume, DownVector, SideVector, maxIterDown, &NbCollisions, min_dist, filters, SWEEP_PASS_DOWN))
		{
			if(NbCollisions)
			{
				if(dir_dot_up<=0.0f)	// PT: fix attempt
					CollisionFlags |= PxControllerCollisionFlag::eCOLLISION_DOWN;

				if(mUserParams.mHandleSlope && !(mFlags & (STF_TOUCH_OTHER_CCT|STF_TOUCH_OBSTACLE)))	// PT: I think the following fix shouldn't be performed when mHandleSlope is false.
				{
					// PT: the following code is responsible for a weird capsule behaviour,
					// when colliding against a highly tesselated terrain:
					// - with a large direction vector, the capsule gets stuck against some part of the terrain
					// - with a slower direction vector (but in the same direction!) the capsule manages to move
					// I will keep that code nonetheless, since it seems to be useful for them.
//printf("%d\n", mFlags & STF_VALIDATE_TRIANGLE_SIDE);
					// constrainedClimbingMode
					if((mFlags & STF_VALIDATE_TRIANGLE_SIDE) && testSlope(mContactNormalSidePass, upDirection, mUserParams.mSlopeLimit))
					{
//printf("%d\n", mFlags & STF_VALIDATE_TRIANGLE_SIDE);
						if(constrainedClimbingMode && mContactPointHeight > originalBottomPoint + stepOffset)
						{
							mFlags |= STF_HIT_NON_WALKABLE;
							if(!(mFlags & STF_WALK_EXPERIMENT))
								return CollisionFlags;
	//						printf("Contrained\n");
						}
					}
					//~constrainedClimbingMode
				}
			}
		}
		//printf("AD:%.2f %.2f %.2f NS=%d\n", volume.mCenter.x, volume.mCenter.y, volume.mCenter.z, mNbCachedStatic);
//		printf("%d\n", mTouchOtherCCT);

		// TEST: do another down pass if we're on a non-walkable poly
		// ### kind of works but still not perfect
		// ### could it be because we zero the Y impulse later?
		// ### also check clamped response vectors
//		if(mUserParams.mHandleSlope && mValidateTriangle && direction[upDirection]<0.0f)
//		if(mUserParams.mHandleSlope && !mTouchOtherCCT  && !mTouchObstacle && mValidateTriangle && dir_dot_up<0.0f)
		if(mUserParams.mHandleSlope && !(mFlags & (STF_TOUCH_OTHER_CCT|STF_TOUCH_OBSTACLE)) && (mFlags & STF_VALIDATE_TRIANGLE_DOWN) && dir_dot_up<=0.0f)
		{
			PxVec3 Normal;
		#ifdef USE_CONTACT_NORMAL_FOR_SLOPE_TEST
			Normal = mContactNormalDownPass;
		#else
			//mTouchedTriangle.normal(Normal);
			Normal = mContactNormalDownPass;
		#endif

			const float touchedTriHeight = float(mTouchedTriMax - originalBottomPoint);

/*			if(touchedTriHeight>mUserParams.mStepOffset)
			{
				if(constrainedClimbingMode && mContactPointHeight > originalBottomPoint + stepOffset)
				{
					mFlags |= STF_HIT_NON_WALKABLE;
					if(!(mFlags & STF_WALK_EXPERIMENT))
						return CollisionFlags;
				}
			}*/

			if(touchedTriHeight>mUserParams.mStepOffset && testSlope(Normal, upDirection, mUserParams.mSlopeLimit))
			{
				mFlags |= STF_HIT_NON_WALKABLE;
				// Early exit if we're going to run this again anyway...
				if(!(mFlags & STF_WALK_EXPERIMENT))
					return CollisionFlags;
		/*		CatchScene()->GetRenderer()->AddLine(mTouchedTriangle.mVerts[0], mTouched.mVerts[1], ARGB_YELLOW);
				CatchScene()->GetRenderer()->AddLine(mTouchedTriangle.mVerts[0], mTouched.mVerts[2], ARGB_YELLOW);
				CatchScene()->GetRenderer()->AddLine(mTouchedTriangle.mVerts[1], mTouched.mVerts[2], ARGB_YELLOW);
		*/

				// ==========[ WALK EXPERIMENT ]===========================

				mFlags |= STF_NORMALIZE_RESPONSE;

				const PxExtended tmp = toVec3(volume.mCenter).dot(upDirection);
				PxExtended Delta = tmp > originalHeight ? tmp - originalHeight : 0.0f;
//				PxExtended Delta = volume.mCenter[upDirection] > originalHeight ? volume.mCenter[upDirection] - originalHeight : 0.0f;
				Delta += fabsf(direction.dot(upDirection));
//				Delta += fabsf(direction[upDirection]);
				PxExtended Recover = Delta;

				NbCollisions=0;
				const PxExtended MD = Recover < min_dist ? Recover/float(maxIter) : min_dist;

				PxVec3 RecoverPoint(0,0,0);
//				RecoverPoint[upDirection]=-float(Recover);
				RecoverPoint = -upDirection*float(Recover);

				// PT: we pass "SWEEP_PASS_UP" for compatibility with previous code, but it's technically wrong (this is a 'down' pass)
				if(doSweepTest(userData, userHitData, userObstacles, volume, RecoverPoint, SideVector, maxIter, &NbCollisions, float(MD), filters, SWEEP_PASS_UP))
				{
		//			if(NbCollisions)	CollisionFlags |= COLLISION_Y_DOWN;
					// PT: why did we do this ? Removed for now. It creates a bug (non registered event) when we land on a steep poly.
					// However this might have been needed when we were sliding on those polygons, and we didn't want the land anim to
					// start while we were sliding.
		//			if(NbCollisions)	CollisionFlags &= ~PxControllerFlag::eCOLLISION_DOWN;
				}
				mFlags &= ~STF_NORMALIZE_RESPONSE;
			}
		}
	}

	return CollisionFlags;
}
