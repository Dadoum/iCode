//
//  ARFaceGeometry.h
//  ARKit
//
//  Copyright © 2016-2017 Apple Inc. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <SceneKit/SCNGeometry.h>
#import <ARKit/ARFaceAnchor.h>

@protocol MTLBuffer;
@protocol MTLDevice;
@class ARFaceAnchor;

NS_ASSUME_NONNULL_BEGIN

/**
 An object representing the geometry of a face.
 */
API_AVAILABLE(ios(11.0)) API_UNAVAILABLE(macos, watchos, tvos)
@interface ARFaceGeometry : NSObject<NSCopying>

/**
The number of mesh vertices of the geometry.
 */
@property (nonatomic, readonly) NSUInteger vertexCount NS_REFINED_FOR_SWIFT;

/**
 The mesh vertices of the geometry.
 */
@property (nonatomic, readonly) const vector_float3 *vertices NS_REFINED_FOR_SWIFT;

/**
 The number of texture coordinates of the face geometry.
 */
@property (nonatomic, readonly) NSUInteger textureCoordinateCount NS_REFINED_FOR_SWIFT;

/**
 The texture coordinates of the geometry.
 */
@property (nonatomic, readonly) const vector_float2 *textureCoordinates NS_REFINED_FOR_SWIFT;

/**
 The number of triangles of the face geometry.
 */
@property (nonatomic, readonly) NSUInteger triangleCount;

/**
 The triangle indices of the geometry.
 */
@property (nonatomic, readonly) const int16_t *triangleIndices NS_REFINED_FOR_SWIFT;

/**
 Creates and returns a face geometry by applying a set of given blend shape coefficients.
 
 @discussion An empty dictionary can be provided to create a neutral face geometry.
 @param blendShapes A dictionary of blend shape coefficients.
 @return Face geometry after applying the blend shapes.
 */
- (nullable instancetype)initWithBlendShapes:(NSDictionary<ARBlendShapeLocation, NSNumber*> *)blendShapes;

/** Unavailable */
- (instancetype)init NS_UNAVAILABLE;
+ (instancetype)new NS_UNAVAILABLE;

@end


/**
 A SceneKit geometry representing a face.
 */
API_AVAILABLE(ios(11.0)) API_UNAVAILABLE(macos, watchos, tvos)
@interface ARSCNFaceGeometry : SCNGeometry

/**
 Creates a new face geometry using a metal device.
 
 @param device A metal device.
 @return A new face geometry
 */
+ (nullable instancetype)faceGeometryWithDevice:(id<MTLDevice>)device;


/**
 Creates a new face geometry using a metal device.
 
 @discussion By default the regions between the eye lids as well as the region
 between the lips are not covered by geometry. For using the face geometry as an
 occlusion geometry set \p fillMesh to YES. This will fill
 in additional geometry into the gaps between the eye lids as well as into the
 gap between the lips.
 @param fillMesh Whether to fill in additional geometry into the
 gaps between the eye lids as well as into the gap between the lips.
 
 @return A new face geometry
 */
+ (nullable instancetype)faceGeometryWithDevice:(id<MTLDevice>)device
                              fillMesh:(BOOL)fillMesh;


/**
 Updates the geometry with the vertices of a face geometry.
 
 @param faceGeometry A face geometry.
 */
- (void)updateFromFaceGeometry:(ARFaceGeometry *)faceGeometry;

/** Unavailable */
- (instancetype)init NS_UNAVAILABLE;
+ (instancetype)new NS_UNAVAILABLE;

@end

NS_ASSUME_NONNULL_END

